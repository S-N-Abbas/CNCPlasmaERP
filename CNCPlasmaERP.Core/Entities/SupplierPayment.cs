using System;
using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>A payment made to a supplier against the running account.</summary>
    public class SupplierPayment : BaseEntity
    {
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public int? PaidByUserId { get; set; }
        public User? PaidByUser { get; set; }
    }
}
