using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
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

        public StockService(IUnitOfWork unitOfWork, ILogger<StockService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task RecordPurchaseAsync(StockPurchase purchase)
        {
            var material = await _unitOfWork.Materials.GetByIdAsync(purchase.MaterialId)
                ?? throw new KeyNotFoundException($"Material {purchase.MaterialId} not found.");

            purchase.MaterialCost = purchase.WeightKg * purchase.RatePerKg;

            await _unitOfWork.StockPurchases.AddAsync(purchase);

            material.CurrentStockKg += purchase.WeightKg;
            if (purchase.SheetCount.HasValue)
            {
                material.CurrentStockCount = (material.CurrentStockCount ?? 0) + purchase.SheetCount.Value;
            }

            _unitOfWork.Materials.Update(material);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Stock purchase recorded: {WeightKg} Kg of Material {MaterialId} at {RatePerKg}/Kg from Supplier {SupplierId}. New balance: {NewBalanceKg} Kg",
                purchase.WeightKg, purchase.MaterialId, purchase.RatePerKg, purchase.SupplierId, material.CurrentStockKg);
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

            _logger.LogInformation(
                "Job {JobId} consumed {WeightKg} Kg of Material {MaterialId} at cost {CostRate}/Kg, sold at {SellRate}/Kg (scrap generated: {ScrapKg} Kg)",
                jobId, weightUsedKg, materialId, costRatePerKg, sellingRatePerKg, scrapGeneratedKg);

            return usage;
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
                .FirstOrDefaultAsync();

            return lastPurchase?.RatePerKg ?? material.DefaultRatePerKg ?? 0m;
        }

        private async Task AccrueScrapAsync(int materialId, decimal scrapKg)
        {
            var scrap = await _unitOfWork.Scraps.FirstOrDefaultAsync(s => s.MaterialId == materialId);

            if (scrap is null)
            {
                scrap = new Scrap { MaterialId = materialId, BalanceKg = 0 };
                await _unitOfWork.Scraps.AddAsync(scrap);
            }

            scrap.BalanceKg += scrapKg;
            _unitOfWork.Scraps.Update(scrap);
        }

        public async Task RecordScrapSaleAsync(ScrapSale sale)
        {
            var scrap = await _unitOfWork.Scraps.FirstOrDefaultAsync(s => s.MaterialId == sale.MaterialId)
                ?? throw new InsufficientScrapException(sale.MaterialId, sale.WeightKgSold, 0);

            if (scrap.BalanceKg < sale.WeightKgSold)
            {
                throw new InsufficientScrapException(sale.MaterialId, sale.WeightKgSold, scrap.BalanceKg);
            }

            sale.TotalAmount = sale.WeightKgSold * sale.RatePerKg;

            scrap.BalanceKg -= sale.WeightKgSold;
            _unitOfWork.Scraps.Update(scrap);

            await _unitOfWork.ScrapSales.AddAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Scrap sale recorded: {WeightKg} Kg of Material {MaterialId} sold to {Buyer} for {Amount}",
                sale.WeightKgSold, sale.MaterialId, sale.BuyerName, sale.TotalAmount);
        }

        public async Task<IReadOnlyList<Material>> GetLowStockMaterialsAsync() =>
            await _unitOfWork.Materials.Query()
                .Where(m => m.IsActive && m.ReorderLevelKg != null && m.CurrentStockKg <= m.ReorderLevelKg)
                .ToListAsync();
    }
}
