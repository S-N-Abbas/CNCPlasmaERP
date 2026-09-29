using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Owns every rule around stock quantities: purchases increase it, job
    /// consumption decreases it (and accrues scrap), scrap sales reduce the scrap
    /// balance. No other layer should mutate Material.CurrentStockKg/Count or
    /// Scrap.BalanceKg directly — always go through this service so the numbers
    /// stay consistent and every movement is logged.
    /// </summary>
    public interface IStockService
    {
        /// <summary>Records a goods-receipt from a supplier and increases the material's stock.</summary>
        Task RecordPurchaseAsync(StockPurchase purchase);

        /// <summary>
        /// Records material consumed by a job. Validates sufficient stock (throws
        /// InsufficientStockException otherwise), decreases stock, snapshots the
        /// current cost rate, and accrues any leftover as scrap.
        /// </summary>
        Task<JobMaterialUsage> RecordJobMaterialUsageAsync(
            int jobId,
            int materialId,
            decimal weightUsedKg,
            int? sheetsUsed,
            decimal sellingRatePerKg,
            decimal scrapGeneratedKg,
            string? notes);


        /// <summary>
        /// Reverses a previously recorded job material usage: restores the consumed
        /// weight/sheets back to stock and removes the corresponding scrap accrual.
        /// Throws CannotReverseUsageException if the accrued scrap has already been
        /// partially or fully sold (so it can no longer be cleanly subtracted back).
        /// </summary>
        Task ReverseJobMaterialUsageAsync(int jobMaterialUsageId);


        /// <summary>Records a sale of accumulated scrap. Throws InsufficientScrapException if the balance is too low.</summary>
        Task RecordScrapSaleAsync(ScrapSale sale);

        /// <summary>Active materials at or below their reorder level — feeds the dashboard's low-stock widget.</summary>
        Task<IReadOnlyList<Material>> GetLowStockMaterialsAsync();
    }
}
