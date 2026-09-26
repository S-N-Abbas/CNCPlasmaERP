using System;
using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// A payment received from a customer. May be tied to a specific Invoice,
    /// or left unlinked (InvoiceId = null, IsAdvance = true) to represent an
    /// advance/on-account payment applied against future invoices.
    /// </summary>
    public class Payment : BaseEntity
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        /// <summary>Null when this is an advance/on-account payment not yet applied to an invoice.</summary>
        public int? InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? ReferenceNumber { get; set; }
        public bool IsAdvance { get; set; }
        public string? Notes { get; set; }

        public int? ReceivedByUserId { get; set; }
        public User? ReceivedByUser { get; set; }
    }
}
