using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.Core.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    /// <summary>
    /// The single place where Material.CurrentStockKg/CurrentStockCount and
    /// Scrap.BalanceKg are ever changed. Keeping every stock movement funnelled
    /// through one service means the numbers can never drift out of sync with the
    /// underlying purchase/usage/sale records, and every movement gets logged.
    /// </summary>
    public class StockService : IStockService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StockService> _logger;
        private readonly ICurrentUserContext _currentUserContext;

        public StockService(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext, ILogger<StockService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserContext = currentUserContext;
            _logger = logger;
        }

        public async Task RecordPurchaseAsync(StockPurchase purchase)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageStock);
            if (purchase.WeightKg <= 0 || purchase.RatePerKg <= 0 || purchase.CarriageCost < 0 || purchase.SheetCount < 0)
                throw new BusinessValidationException("Purchase weight and rate must be positive; carriage and sheet count cannot be negative.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

            var material = await _unitOfWork.Materials.GetByIdAsync(purchase.MaterialId)
                ?? throw new KeyNotFoundException($"Material {purchase.MaterialId} not found.");

            purchase.MaterialCost = System.Math.Round(purchase.WeightKg * purchase.RatePerKg, 2, System.MidpointRounding.AwayFromZero);

            await _unitOfWork.StockPurchases.AddAsync(purchase);

            material.CurrentStockKg += purchase.WeightKg;
            if (purchase.SheetCount.HasValue)
            {
                material.CurrentStockCount = (material.CurrentStockCount ?? 0) + purchase.SheetCount.Value;
            }

            _unitOfWork.Materials.Update(material);

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Stock purchase recorded: {WeightKg} Kg of Material {MaterialId} at {RatePerKg}/Kg from Supplier {SupplierId}. New balance: {NewBalanceKg} Kg",
                purchase.WeightKg, purchase.MaterialId, purchase.RatePerKg, purchase.SupplierId, material.CurrentStockKg);
        }

        public async Task<IReadOnlyList<StockPurchase>> GetRecentPurchasesAsync(int count = 50) =>
            await GetPurchasesAuthorizedAsync(count);

        private async Task<IReadOnlyList<StockPurchase>> GetPurchasesAuthorizedAsync(int count)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageStock);
            return await _unitOfWork.StockPurchases.Query()
            .Include(p => p.Material)
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(count)
            .ToListAsync();
        }

        public async Task<JobMaterialUsage> RecordJobMaterialUsageAsync(
            int jobId,
            int materialId,
            decimal weightUsedKg,
            int? sheetsUsed,
            decimal sellingRatePerKg,
            decimal scrapGeneratedKg,
            string? notes)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageJobs);
            if (weightUsedKg <= 0 || sellingRatePerKg <= 0 || scrapGeneratedKg < 0 || sheetsUsed < 0)
                throw new BusinessValidationException("Material weight and selling rate must be positive; scrap and sheet count cannot be negative.");
            if (scrapGeneratedKg > weightUsedKg)
                throw new BusinessValidationException("Scrap generated cannot exceed the material weight used.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

            var material = await _unitOfWork.Materials.GetByIdAsync(materialId)
                ?? throw new KeyNotFoundException($"Material {materialId} not found.");

            if (material.CurrentStockKg < weightUsedKg)
            {
                _logger.LogWarning(
                    "Job {JobId} attempted to use {RequestedKg} Kg of Material {MaterialId} but only {AvailableKg} Kg is in stock",
                    jobId, weightUsedKg, materialId, material.CurrentStockKg);
                throw new InsufficientStockException(materialId, weightUsedKg, material.CurrentStockKg);
            }

            if (sheetsUsed.HasValue && material.CurrentStockCount.HasValue &&
                material.CurrentStockCount.Value < sheetsUsed.Value)
            {
                throw new InsufficientStockException(materialId, sheetsUsed.Value, material.CurrentStockCount.Value);
            }

            var costRatePerKg = await GetCurrentCostRateAsync(materialId, material);

            var usage = new JobMaterialUsage
            {
                JobId = jobId,
                MaterialId = materialId,
                WeightUsedKg = weightUsedKg,
                SheetsUsed = sheetsUsed,
                CostRatePerKg = costRatePerKg,
                SellingRatePerKg = sellingRatePerKg,
                ScrapGeneratedKg = scrapGeneratedKg,
                Notes = notes
            };

            await _unitOfWork.JobMaterialUsages.AddAsync(usage);

            material.CurrentStockKg -= weightUsedKg;
            if (sheetsUsed.HasValue && material.CurrentStockCount.HasValue)
            {
                material.CurrentStockCount -= sheetsUsed.Value;
            }

            _unitOfWork.Materials.Update(material);

            if (scrapGeneratedKg > 0)
            {
                await AccrueScrapAsync(materialId, scrapGeneratedKg);
            }

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Job {JobId} consumed {WeightKg} Kg of Material {MaterialId} at cost {CostRate}/Kg, sold at {SellRate}/Kg (scrap generated: {ScrapKg} Kg)",
                jobId, weightUsedKg, materialId, costRatePerKg, sellingRatePerKg, scrapGeneratedKg);

            return usage;
        }


        public async Task ReverseJobMaterialUsageAsync(int jobMaterialUsageId)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageJobs);
            await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
            var usage = await _unitOfWork.JobMaterialUsages.GetByIdAsync(jobMaterialUsageId)
                ?? throw new KeyNotFoundException($"Job material usage {jobMaterialUsageId} not found.");

            var material = await _unitOfWork.Materials.GetByIdAsync(usage.MaterialId)
                ?? throw new KeyNotFoundException($"Material {usage.MaterialId} not found.");

            if (usage.ScrapGeneratedKg > 0)
            {
                var scrap = await _unitOfWork.Scraps.FirstOrDefaultAsync(s => s.MaterialId == usage.MaterialId);

                if (scrap is null || scrap.BalanceKg < usage.ScrapGeneratedKg)
                {
                    var available = scrap?.BalanceKg ?? 0;
                    _logger.LogWarning(
                        "Cannot reverse usage {UsageId}: {ScrapKg} Kg of scrap was accrued but only {AvailableKg} Kg remains (some has likely been sold)",
                        jobMaterialUsageId, usage.ScrapGeneratedKg, available);
                    throw new CannotReverseUsageException(
                        $"Cannot reverse this usage — {usage.ScrapGeneratedKg:N3} Kg of scrap was generated but only {available:N3} Kg remains unsold.");
                }

                scrap.BalanceKg -= usage.ScrapGeneratedKg;
                _unitOfWork.Scraps.Update(scrap);
            }

            material.CurrentStockKg += usage.WeightUsedKg;
            if (usage.SheetsUsed.HasValue && material.CurrentStockCount.HasValue)
            {
                material.CurrentStockCount += usage.SheetsUsed.Value;
            }
            _unitOfWork.Materials.Update(material);

            _unitOfWork.JobMaterialUsages.Remove(usage); // soft-deleted via BaseEntity — audit trail preserved

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Reversed job material usage {UsageId}: restored {WeightKg} Kg to Material {MaterialId}",
                jobMaterialUsageId, usage.WeightUsedKg, usage.MaterialId);
        }

        /// <summary>
        /// Cost basis for P&amp;L: uses the rate from this material's most recent
        /// purchase (a simple, transparent "last-cost" approach rather than a
        /// moving weighted average, which is easier for the shop owner to verify
        /// against a physical purchase invoice). Falls back to the material's
        /// default estimated rate only if it has somehow never been purchased.
        /// </summary>
        private async Task<decimal> GetCurrentCostRateAsync(int materialId, Material material)
        {
            var lastPurchase = await _unitOfWork.StockPurchases.Query()
                .Where(p => p.MaterialId == materialId)
                .OrderByDescending(p => p.PurchaseDate)
                .ThenByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            return lastPurchase?.RatePerKg ?? material.DefaultRatePerKg ?? 0m;
        }

        private async Task AccrueScrapAsync(int materialId, decimal scrapKg)
        {
            var scrap = await _unitOfWork.Scraps.FirstOrDefaultAsync(s => s.MaterialId == materialId);

            if (scrap is null)
            {
                // First scrap ever recorded for this material: create the row with its
                // starting balance. Do NOT call Update() on it afterwards - a brand-new
                // row has only a temporary key until SaveChanges runs.
                await _unitOfWork.Scraps.AddAsync(new Scrap { MaterialId = materialId, BalanceKg = scrapKg });
                return;
            }

            // Existing row is already tracked (FirstOrDefaultAsync returns a tracked
            // entity), so EF Core's change detection picks this up on SaveChanges.
            scrap.BalanceKg += scrapKg;
        }

        public async Task RecordScrapSaleAsync(ScrapSale sale)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageStock);
            if (sale.WeightKgSold <= 0 || sale.RatePerKg <= 0)
                throw new BusinessValidationException("Scrap sale weight and rate must be greater than zero.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

            var scrap = await _unitOfWork.Scraps.FirstOrDefaultAsync(s => s.MaterialId == sale.MaterialId)
                ?? throw new InsufficientScrapException(sale.MaterialId, sale.WeightKgSold, 0);

            if (scrap.BalanceKg < sale.WeightKgSold)
            {
                throw new InsufficientScrapException(sale.MaterialId, sale.WeightKgSold, scrap.BalanceKg);
            }

            sale.TotalAmount = System.Math.Round(sale.WeightKgSold * sale.RatePerKg, 2, System.MidpointRounding.AwayFromZero);

            scrap.BalanceKg -= sale.WeightKgSold;
            _unitOfWork.Scraps.Update(scrap);

            await _unitOfWork.ScrapSales.AddAsync(sale);

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Scrap sale recorded: {WeightKg} Kg of Material {MaterialId} sold to {Buyer} for {Amount}",
                sale.WeightKgSold, sale.MaterialId, sale.BuyerName, sale.TotalAmount);
        }

        public async Task<IReadOnlyList<Material>> GetLowStockMaterialsAsync()
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageStock);
            return await _unitOfWork.Materials.Query()
                .Where(m => m.IsActive && m.ReorderLevelKg != null && m.CurrentStockKg <= m.ReorderLevelKg)
                .ToListAsync();
        }
    }
}
