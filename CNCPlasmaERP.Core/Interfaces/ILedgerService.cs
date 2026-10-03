using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Owns every posting to a customer's running account. Customer.CurrentBalance
    /// should never be modified anywhere except through this service, so the
    /// denormalized balance always matches the sum of its CustomerLedgerEntry rows.
    /// </summary>
    public interface ILedgerService
    {
        /// <summary>Posts a debit for a newly raised invoice. Call once, right after the invoice itself is saved.</summary>
        Task PostInvoiceAsync(Invoice invoice);

        /// <summary>
        /// Saves the payment, applies it against the linked invoice's PaidAmount/Status
        /// if InvoiceId is set, and posts the corresponding credit to the ledger.
        /// </summary>
        Task PostPaymentAsync(Payment payment);

        /// <summary>Manual correction entry (e.g. a reconciliation adjustment) — used sparingly, always with a note explaining why.</summary>
        Task PostAdjustmentAsync(int customerId, decimal debit, decimal credit, string? notes);

        /// <summary>Posts the one-time opening balance entry for a newly added customer. No-op if OpeningBalance is zero.</summary>
        Task PostOpeningBalanceAsync(Customer customer);

        /// <summary>Full or date-filtered ledger history for a customer, oldest first — used for printable statements.</summary>
        Task<IReadOnlyList<CustomerLedgerEntry>> GetStatementAsync(int customerId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>Most recent payments (Customer and Invoice loaded) — for the Payments screen history grid.</summary>
        Task<IReadOnlyList<Payment>> GetRecentPaymentsAsync(int count = 50);
    }
}
