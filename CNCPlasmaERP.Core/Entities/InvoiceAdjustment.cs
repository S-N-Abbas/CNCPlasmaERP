using System;
using CNCPlasmaERP.Core.Common;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>Immutable audit of a correction to invoice charges or discount.</summary>
    public class InvoiceAdjustment : BaseEntity
    {
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;
        public DateTime AdjustmentDate { get; set; } = DateTime.Now;
        public decimal PreviousOtherCharges { get; set; }
        public decimal NewOtherCharges { get; set; }
        public decimal PreviousDiscountAmount { get; set; }
        public decimal NewDiscountAmount { get; set; }
        public decimal PreviousTaxAmount { get; set; }
        public decimal NewTaxAmount { get; set; }
        public decimal PreviousGrandTotal { get; set; }
        public decimal NewGrandTotal { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int? AdjustedByUserId { get; set; }
        public User? AdjustedByUser { get; set; }
    }
}
