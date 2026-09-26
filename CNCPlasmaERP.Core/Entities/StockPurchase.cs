using System;
using System.ComponentModel.DataAnnotations.Schema;
using CNCPlasmaERP.Core.Common;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// A single goods-receipt transaction: sheets bought from a supplier.
    /// Saving this (via StockService) increases the linked Material's
    /// CurrentStockKg / CurrentStockCount.
    /// </summary>
    public class StockPurchase : BaseEntity
    {
        public int MaterialId { get; set; }
        public Material Material { get; set; } = null!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        /// <summary>Weight purchased, in Kg. Primary unit — always required.</summary>
        public decimal WeightKg { get; set; }

        /// <summary>Number of sheets purchased. Secondary/optional — can be skipped for odd lots or off-cuts bought in bulk.</summary>
        public int? SheetCount { get; set; }

        public decimal? SheetLengthFt { get; set; }
        public decimal? SheetWidthFt { get; set; }

        public decimal RatePerKg { get; set; }

        /// <summary>Freight/carriage paid to bring material in. Kept separate from material cost for accurate P&amp;L.</summary>
        public decimal CarriageCost { get; set; }

        /// <summary>WeightKg * RatePerKg, stored (not computed) so historical cost is preserved if rates change later.</summary>
        public decimal MaterialCost { get; set; }

        [NotMapped]
        public decimal TotalCost => MaterialCost + CarriageCost;

        public string? SupplierInvoiceNumber { get; set; }
        public string? Notes { get; set; }

        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
    }
}
