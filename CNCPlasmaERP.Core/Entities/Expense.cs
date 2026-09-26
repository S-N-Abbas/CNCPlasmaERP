using System;
using CNCPlasmaERP.Core.Common;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// General running-cost entry (machine maintenance, electricity, rent, consumables,
    /// worker wages, etc). Wages are recorded here under Category = Wages rather than
    /// a separate table, with PaidTo holding the worker's name — kept intentionally
    /// simple per business requirement, while still giving the Wages screen its own
    /// filtered view over this same table.
    /// </summary>
    public class Expense : BaseEntity
    {
        public DateTime ExpenseDate { get; set; } = DateTime.Now;
        public ExpenseCategory Category { get; set; }
        public decimal Amount { get; set; }

        /// <summary>Worker name (for Wages), vendor name, or payee — free text.</summary>
        public string? PaidTo { get; set; }

        public string? Description { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public string? ReferenceNumber { get; set; }

        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
    }
}
