using System.Collections.Generic;
using CNCPlasmaERP.Core.Common;

namespace CNCPlasmaERP.Core.Entities
{
    public class Customer : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }

        /// <summary>Balance brought forward when the customer was first entered into the system.</summary>
        public decimal OpeningBalance { get; set; }

        /// <summary>Denormalized running balance (positive = customer owes shop). Maintained
        /// by LedgerService and always reconcilable from CustomerLedgerEntry rows.</summary>
        public decimal CurrentBalance { get; set; }

        /// <summary>Optional credit limit for running-account customers; used for warnings, not hard enforcement.</summary>
        public decimal? CreditLimit { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Job> Jobs { get; set; } = new List<Job>();
        public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<CustomerLedgerEntry> LedgerEntries { get; set; } = new List<CustomerLedgerEntry>();
    }
}
