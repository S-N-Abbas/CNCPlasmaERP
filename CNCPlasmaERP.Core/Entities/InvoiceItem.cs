using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Entities
{
    public class InvoiceItem : BaseEntity
    {
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public InvoiceItemType ItemType { get; set; }
        public string Description { get; set; } = string.Empty;

        /// <summary>Only meaningful for ItemType = Material.</summary>
        public decimal? QuantityKg { get; set; }
        public decimal? RatePerKg { get; set; }

        public decimal Amount { get; set; }
    }
}
