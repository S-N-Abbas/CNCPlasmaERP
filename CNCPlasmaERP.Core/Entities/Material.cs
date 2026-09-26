using System.Collections.Generic;
using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// Catalog item representing a distinct material specification
    /// (e.g. "MS Sheet - 16 Gauge"). Acts as the SKU for stock tracking.
    /// Stock quantities are denormalized here for fast lookup and are
    /// maintained by StockService whenever purchases, job usage or
    /// scrap sales occur.
    /// </summary>
    public class Material : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public MaterialType Type { get; set; }

        /// <summary>Free-text gauge/thickness label, e.g. "16G" or "1.5mm".</summary>
        public string Gauge { get; set; } = string.Empty;

        /// <summary>Standard sheet size, optional — used only for reference/estimation.</summary>
        public decimal? StandardLengthFt { get; set; }
        public decimal? StandardWidthFt { get; set; }

        /// <summary>Default/estimated rate per Kg, used for quotations before an actual purchase rate is known.</summary>
        public decimal? DefaultRatePerKg { get; set; }

        /// <summary>Current available stock weight in Kg. Primary stock unit — always accurate. Updated by StockService.</summary>
        public decimal CurrentStockKg { get; set; }

        /// <summary>Current available full-sheet count. Secondary/optional — can be left null where counting sheets doesn't apply.</summary>
        public int? CurrentStockCount { get; set; }

        /// <summary>Reorder threshold in Kg for low-stock alerts on the dashboard.</summary>
        public decimal? ReorderLevelKg { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<StockPurchase> Purchases { get; set; } = new List<StockPurchase>();
        public ICollection<JobMaterialUsage> JobUsages { get; set; } = new List<JobMaterialUsage>();
        public Scrap? ScrapBalance { get; set; }
    }
}
