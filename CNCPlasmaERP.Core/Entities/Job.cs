using System;
using System.Collections.Generic;
using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// A real work order: cutting/bending done for a customer. May consume
    /// multiple materials (via MaterialUsages), each producing its own scrap.
    /// A Job is billed through exactly one Invoice once completed.
    /// </summary>
    public class Job : BaseEntity
    {
        public string JobNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public DateTime JobDate { get; set; } = DateTime.Now;
        public JobStatus Status { get; set; } = JobStatus.Pending;

        /// <summary>Cutting/bending service charge, independent of material cost.</summary>
        public decimal ServiceCharges { get; set; }

        /// <summary>Delivery/carriage charge to the customer, if applicable.</summary>
        public decimal CarriageCharges { get; set; }

        public string? Description { get; set; }
        public string? Notes { get; set; }

        /// <summary>Set if this job originated from a quotation. Purely informational link.</summary>
        public int? QuotationId { get; set; }
        public Quotation? Quotation { get; set; }

        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public ICollection<JobMaterialUsage> MaterialUsages { get; set; } = new List<JobMaterialUsage>();

        public Invoice? Invoice { get; set; }
    }
}
