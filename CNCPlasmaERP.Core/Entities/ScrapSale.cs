using System;
using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// A sale of accumulated scrap to a scrap dealer/buyer.
    /// Reduces Scrap.BalanceKg and contributes as "other income" in P&amp;L.
    /// </summary>
    public class ScrapSale : BaseEntity
    {
        public int MaterialId { get; set; }
        public Material Material { get; set; } = null!;

        public DateTime SaleDate { get; set; } = DateTime.Now;
        public decimal WeightKgSold { get; set; }
        public decimal RatePerKg { get; set; }
        public decimal TotalAmount { get; set; }

        /// <summary>Scrap buyers are usually informal/walk-in; kept as free text rather than a full Customer/Supplier record.</summary>
        public string BuyerName { get; set; } = string.Empty;

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public string? Notes { get; set; }
    }
}
