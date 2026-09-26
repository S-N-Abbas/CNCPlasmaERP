using System;

namespace CNCPlasmaERP.Core.Common
{
    /// <summary>
    /// Common audit fields shared by every entity in the system.
    /// IsDeleted enables soft-delete so historical records (jobs, invoices,
    /// stock movements) are never physically removed, preserving P&amp;L history.
    /// </summary>
    public abstract class BaseEntity
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
