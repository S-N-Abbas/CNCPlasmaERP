using System;
using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// Immutable running-account entry for a customer (invoice raised, payment received,
    /// advance taken, manual adjustment). RunningBalance is a snapshot computed and stored
    /// at insert time so historical statements never change even as later entries are added.
    /// </summary>
    public class CustomerLedgerEntry : BaseEntity
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public DateTime EntryDate { get; set; } = DateTime.Now;
        public TransactionType Type { get; set; }

        /// <summary>Amount customer owes the shop (invoice, or a debit adjustment).</summary>
        public decimal Debit { get; set; }

        /// <summary>Amount received from / credited to the customer (payment, advance, or credit adjustment).</summary>
        public decimal Credit { get; set; }

        /// <summary>Balance immediately after this entry (positive = customer owes shop).</summary>
        public decimal RunningBalance { get; set; }

        public int? InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        public int? PaymentId { get; set; }
        public Payment? Payment { get; set; }

        public string? Notes { get; set; }
    }
}
