using System.ComponentModel.DataAnnotations.Schema;
using CNCPlasmaERP.Core.Common;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// One material consumed within a Job. Cost and selling rates are snapshotted
    /// at the time of use so historical P&amp;L stays accurate even if Material rates
    /// change later. Leftover offcut recorded here feeds directly into Scrap.BalanceKg
    /// via StockService.
    /// </summary>
    public class JobMaterialUsage : BaseEntity
    {
        public int JobId { get; set; }
        public Job Job { get; set; } = null!;

        public int MaterialId { get; set; }
        public Material Material { get; set; } = null!;

        /// <summary>Weight drawn from stock for this job, in Kg. Primary unit — always required.</summary>
        public decimal WeightUsedKg { get; set; }

        /// <summary>Sheets drawn from stock. Secondary/optional — can be skipped.</summary>
        public int? SheetsUsed { get; set; }

        /// <summary>Cost basis snapshot (Material's current rate at time of use) — drives P&amp;L.</summary>
        public decimal CostRatePerKg { get; set; }

        /// <summary>Rate charged to the customer for this material — drives invoicing. May include markup over cost.</summary>
        public decimal SellingRatePerKg { get; set; }

        /// <summary>Leftover offcut generated from this usage, added to the material's Scrap balance.</summary>
        public decimal ScrapGeneratedKg { get; set; }

        public string? Notes { get; set; }

        [NotMapped]
        public decimal CostAmount => WeightUsedKg * CostRatePerKg;

        [NotMapped]
        public decimal SellingAmount => WeightUsedKg * SellingRatePerKg;

        [NotMapped]
        public decimal ProfitAmount => SellingAmount - CostAmount;
    }
}
