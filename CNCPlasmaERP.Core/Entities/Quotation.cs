using System;
using System.Collections.Generic;
using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// A record-only estimate for a customer. Creating or editing a quotation
    /// never touches stock or the customer ledger — it exists purely for reference
    /// and can optionally be converted into a real Job later.
    /// </summary>
    public class Quotation : BaseEntity
    {
        public string QuotationNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public DateTime QuotationDate { get; set; } = DateTime.Now;
        public DateTime? ExpiryDate { get; set; }
        public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

        public decimal EstimatedServiceCharges { get; set; }
        public decimal EstimatedCarriageCharges { get; set; }
        public string? Notes { get; set; }

        /// <summary>Set when this quotation is later turned into an actual Job. Purely informational link.</summary>
        public int? ConvertedToJobId { get; set; }
        public Job? ConvertedToJob { get; set; }

        public ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
    }
}
