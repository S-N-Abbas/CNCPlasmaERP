using System;
using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>Append-only supplier account history. Positive balance means payable.</summary>
    public class SupplierLedgerEntry : BaseEntity
    {
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
        public DateTime EntryDate { get; set; } = DateTime.Now;
        public SupplierTransactionType Type { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
        public int? StockPurchaseId { get; set; }
        public StockPurchase? StockPurchase { get; set; }
        public int? SupplierPaymentId { get; set; }
        public SupplierPayment? SupplierPayment { get; set; }
        public string? Notes { get; set; }
    }
}
