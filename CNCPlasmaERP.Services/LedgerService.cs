using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    /// <summary>
    /// The single place where Customer.CurrentBalance and CustomerLedgerEntry rows
    /// are ever written. Every method stages its changes and calls SaveChangesAsync
    /// exactly once, so each posting is atomic on its own, and safely composes when
    /// called from within a larger transaction owned by another service (e.g.
    /// InvoiceService wraps PostInvoiceAsync inside its own transaction).
    /// </summary>
    public class LedgerService : ILedgerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LedgerService> _logger;

        public LedgerService(IUnitOfWork unitOfWork, ILogger<LedgerService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task PostInvoiceAsync(Invoice invoice)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(invoice.CustomerId)
                ?? throw new KeyNotFoundException($"Customer {invoice.CustomerId} not found.");

            var newBalance = customer.CurrentBalance + invoice.GrandTotal;

            var entry = new CustomerLedgerEntry
            {
                CustomerId = invoice.CustomerId,
                EntryDate = invoice.InvoiceDate,
                Type = TransactionType.Invoice,
                Debit = invoice.GrandTotal,
                Credit = 0,
                RunningBalance = newBalance,
                InvoiceId = invoice.Id,
                Notes = $"Invoice {invoice.InvoiceNumber}"
            };

            await _unitOfWork.LedgerEntries.AddAsync(entry);

            customer.CurrentBalance = newBalance;
            _unitOfWork.Customers.Update(customer);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Posted Invoice {InvoiceNumber} ({Amount}) to Customer {CustomerId} ledger. New balance: {Balance}",
                invoice.InvoiceNumber, invoice.GrandTotal, invoice.CustomerId, newBalance);
        }

        public async Task PostPaymentAsync(Payment payment)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(payment.CustomerId)
                ?? throw new KeyNotFoundException($"Customer {payment.CustomerId} not found.");

            string notes;

            if (payment.InvoiceId.HasValue)
            {
                var invoice = await _unitOfWork.Invoices.GetByIdAsync(payment.InvoiceId.Value)
                    ?? throw new KeyNotFoundException($"Invoice {payment.InvoiceId.Value} not found.");

                invoice.PaidAmount += payment.Amount;
                invoice.Status = invoice.PaidAmount >= invoice.GrandTotal
                    ? InvoiceStatus.Paid
                    : invoice.PaidAmount > 0
                        ? InvoiceStatus.PartiallyPaid
                        : InvoiceStatus.Unpaid;

                _unitOfWork.Invoices.Update(invoice);

                notes = $"Payment against Invoice {invoice.InvoiceNumber}";
            }
            else
            {
                notes = "Advance payment received";
            }

            // Assigned via navigation (not payment.Id, which is still 0 pre-save) so
            // EF Core's relationship fixup resolves the real PaymentId at SaveChanges time.
            await _unitOfWork.Payments.AddAsync(payment);

            var newBalance = customer.CurrentBalance - payment.Amount;

            var entry = new CustomerLedgerEntry
            {
                CustomerId = payment.CustomerId,
                EntryDate = payment.PaymentDate,
                Type = payment.IsAdvance ? TransactionType.Advance : TransactionType.Payment,
                Debit = 0,
                Credit = payment.Amount,
                RunningBalance = newBalance,
                Payment = payment,
                Notes = notes
            };

            await _unitOfWork.LedgerEntries.AddAsync(entry);

            customer.CurrentBalance = newBalance;
            _unitOfWork.Customers.Update(customer);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Posted payment of {Amount} from Customer {CustomerId} ({Notes}). New balance: {Balance}",
                payment.Amount, payment.CustomerId, notes, newBalance);
        }

        public async Task PostAdjustmentAsync(int customerId, decimal debit, decimal credit, string? notes)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId)
                ?? throw new KeyNotFoundException($"Customer {customerId} not found.");

            var newBalance = customer.CurrentBalance + debit - credit;

            var entry = new CustomerLedgerEntry
            {
                CustomerId = customerId,
                EntryDate = DateTime.Now,
                Type = TransactionType.Adjustment,
                Debit = debit,
                Credit = credit,
                RunningBalance = newBalance,
                Notes = notes
            };

            await _unitOfWork.LedgerEntries.AddAsync(entry);

            customer.CurrentBalance = newBalance;
            _unitOfWork.Customers.Update(customer);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Posted manual adjustment for Customer {CustomerId}: Debit {Debit}, Credit {Credit}. New balance: {Balance}",
                customerId, debit, credit, newBalance);
        }

        public async Task PostOpeningBalanceAsync(Customer customer)
        {
            if (customer.OpeningBalance == 0)
            {
                return;
            }

            var entry = new CustomerLedgerEntry
            {
                CustomerId = customer.Id,
                EntryDate = DateTime.Now,
                Type = TransactionType.OpeningBalance,
                Debit = customer.OpeningBalance > 0 ? customer.OpeningBalance : 0,
                Credit = customer.OpeningBalance < 0 ? -customer.OpeningBalance : 0,
                RunningBalance = customer.OpeningBalance,
                Notes = "Opening balance"
            };

            await _unitOfWork.LedgerEntries.AddAsync(entry);

            customer.CurrentBalance = customer.OpeningBalance;
            _unitOfWork.Customers.Update(customer);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Posted opening balance {Balance} for Customer {CustomerId}",
                customer.OpeningBalance, customer.Id);
        }

        public async Task<IReadOnlyList<CustomerLedgerEntry>> GetStatementAsync(
            int customerId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _unitOfWork.LedgerEntries.Query().Where(e => e.CustomerId == customerId);

            if (fromDate.HasValue)
            {
                query = query.Where(e => e.EntryDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(e => e.EntryDate <= toDate.Value);
            }

            return await query
                .OrderBy(e => e.EntryDate)
                .ThenBy(e => e.Id)
                .ToListAsync();
        }
    }
}
