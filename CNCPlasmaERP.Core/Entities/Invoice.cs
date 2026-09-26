using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// Printable A5 invoice raised against a completed Job. Line items (material,
    /// service, carriage) live in InvoiceItem so the layout stays flexible and
    /// supports jobs that used several different materials.
    /// </summary>
    public class Invoice : BaseEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;

        public int JobId { get; set; }
        public Job Job { get; set; } = null!;

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        public decimal SubTotal { get; set; }

        public bool TaxApplied { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }

        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }

        [NotMapped]
        public decimal BalanceAmount => GrandTotal - PaidAmount;

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
        public string? Notes { get; set; }

        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
