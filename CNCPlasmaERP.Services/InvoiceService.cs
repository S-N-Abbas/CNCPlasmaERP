using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace CNCPlasmaERP.Services
{
    /// <summary>
    /// Turns a completed Job into a printable Invoice: one InvoiceItem per material
    /// used (at its snapshotted selling rate), plus service and carriage charges,
    /// tax applied per current AppSettings. The whole operation — invoice creation,
    /// marking the Job as Invoiced, and posting the ledger debit — runs inside a
    /// single transaction so a failure partway through never leaves an invoice
    /// without a matching ledger entry, or vice versa.
    /// </summary>
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILedgerService _ledgerService;
        private readonly INumberSequenceService _numberSequenceService;
        private readonly ILogger<InvoiceService> _logger;
        private readonly ICurrentUserContext _currentUserContext;

        public InvoiceService(
            IUnitOfWork unitOfWork,
            ILedgerService ledgerService,
            INumberSequenceService numberSequenceService,
            ICurrentUserContext currentUserContext,
            ILogger<InvoiceService> logger)
        {
            _unitOfWork = unitOfWork;
            _ledgerService = ledgerService;
            _numberSequenceService = numberSequenceService;
            _currentUserContext = currentUserContext;
            _logger = logger;
        }

        public async Task<Invoice> CreateInvoiceFromJobAsync(int jobId, int? createdByUserId, decimal otherCharges = 0m, decimal discountAmount = 0m)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageInvoicesPayments);
            if (otherCharges < 0 || discountAmount < 0)
                throw new BusinessValidationException("Other charges and discount cannot be negative.");
            otherCharges = Math.Round(otherCharges, 2, MidpointRounding.AwayFromZero);
            discountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero);
            // Clean slate: clears any entity left tracked by an earlier,
            // unrelated operation in this shared session-wide DbContext before
            // this operation touches anything.
            _unitOfWork.DetachAllTrackedEntities();

            var job = await _unitOfWork.Jobs.GetWithDetailsAsync(jobId)
                ?? throw new KeyNotFoundException($"Job {jobId} not found.");

            if (job.Status != JobStatus.Completed)
                throw new InvalidJobStateException($"Job {job.JobNumber} must be completed before it can be invoiced.");

            if (job.Invoice is not null)
            {
                throw new InvalidJobStateException(
                    $"Job {job.JobNumber} has already been invoiced (Invoice {job.Invoice.InvoiceNumber}).");
            }

            if (!job.MaterialUsages.Any() && job.ServiceCharges == 0 && job.CarriageCharges == 0)
            {
                throw new InvalidJobStateException(
                    $"Job {job.JobNumber} has nothing to bill — add material usage, service or carriage charges first.");
            }

            var settings = await _unitOfWork.Settings.GetSettingsAsync();

            await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var items = BuildInvoiceItems(job);

                var subTotal = items.Sum(i => i.Amount);
                var taxApplied = settings.TaxEnabled;
                var taxPercentage = taxApplied ? settings.TaxPercentage : 0m;
                if (discountAmount > subTotal + otherCharges)
                    throw new BusinessValidationException("Discount cannot exceed the invoice subtotal plus other charges.");
                var taxableAmount = subTotal + otherCharges - discountAmount;
                var taxAmount = Math.Round(taxableAmount * taxPercentage / 100m, 2, MidpointRounding.AwayFromZero);
                var grandTotal = taxableAmount + taxAmount;
                if (grandTotal <= 0)
                    throw new BusinessValidationException("Invoice total must be greater than zero.");

                var invoiceNumber = await _numberSequenceService.GenerateInvoiceNumberAsync();

                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    JobId = job.Id,
                    CustomerId = job.CustomerId,
                    InvoiceDate = DateTime.Now,
                    SubTotal = subTotal,
                    OtherCharges = otherCharges,
                    DiscountAmount = discountAmount,
                    TaxApplied = taxApplied,
                    TaxPercentage = taxPercentage,
                    TaxAmount = taxAmount,
                    GrandTotal = grandTotal,
                    PaidAmount = 0,
                    Status = InvoiceStatus.Unpaid,
                    CreatedByUserId = createdByUserId,
                    Items = items
                };

                await _unitOfWork.Invoices.AddAsync(invoice);

                // job (above) was loaded AsNoTracking for safe reading — it must
                // never be handed to Update() directly, since a different,
                // already-tracked instance for the same Id may exist elsewhere
                // in this shared DbContext (e.g. from a JobService call earlier
                // in the session), which would throw an identity-map conflict.
                // Fetching via GetByIdAsync (tracked) reuses that existing
                // instance if present, or tracks a fresh one if not — either way
                // there is only ever one tracked instance per Id.
                var trackedJob = await _unitOfWork.Jobs.GetByIdAsync(job.Id)
                    ?? throw new KeyNotFoundException($"Job {job.Id} not found.");
                trackedJob.Status = JobStatus.Invoiced;
                // No explicit Update() call needed — trackedJob is already
                // tracked, so EF Core's automatic change detection picks up the
                // Status change on SaveChanges.

                await _unitOfWork.SaveChangesAsync();

                // Runs inside the same ambient transaction — LedgerService only calls
                // SaveChangesAsync, never BeginTransactionAsync, so it composes safely here.
                await _ledgerService.PostInvoiceAsync(invoice);

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Invoice {InvoiceNumber} created for Job {JobNumber} (Customer {CustomerId}), GrandTotal {GrandTotal}",
                    invoiceNumber, job.JobNumber, job.CustomerId, grandTotal);

                return invoice;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // The rollback above only undoes the DATABASE changes — EF Core
                // does not automatically revert in-memory tracked entity state.
                // Without this, the half-finished Invoice/Job changes would stay
                // tracked and could corrupt whatever operation runs next.
                _unitOfWork.DetachAllTrackedEntities();

                _logger.LogError(ex, "Failed to create invoice for Job {JobId}; transaction rolled back", jobId);
                throw;
            }
        }

        private static List<InvoiceItem> BuildInvoiceItems(Job job)
        {
            var items = new List<InvoiceItem>();

            foreach (var usage in job.MaterialUsages)
            {
                items.Add(new InvoiceItem
                {
                    ItemType = InvoiceItemType.Material,
                    Description = $"{usage.Material.Name} ({usage.Material.Gauge})",
                    QuantityKg = usage.WeightUsedKg,
                    RatePerKg = usage.SellingRatePerKg,
                    Amount = Math.Round(usage.SellingAmount, 2, MidpointRounding.AwayFromZero)
                });
            }

            if (job.ServiceCharges > 0)
            {
                items.Add(new InvoiceItem
                {
                    ItemType = InvoiceItemType.Service,
                    Description = "Cutting / Bending Service Charges",
                    Amount = Math.Round(job.ServiceCharges, 2, MidpointRounding.AwayFromZero)
                });
            }

            if (job.CarriageCharges > 0)
            {
                items.Add(new InvoiceItem
                {
                    ItemType = InvoiceItemType.Carriage,
                    Description = "Carriage / Delivery Charges",
                    Amount = Math.Round(job.CarriageCharges, 2, MidpointRounding.AwayFromZero)
                });
            }

            return items;
        }

        public Task<Invoice?> GetInvoiceWithDetailsAsync(int invoiceId)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageInvoicesPayments);
            return _unitOfWork.Invoices.GetWithDetailsAsync(invoiceId);
        }

        public Task<IReadOnlyList<Invoice>> GetAllInvoicesAsync()
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageInvoicesPayments);
            return _unitOfWork.Invoices.GetAllWithCustomerAsync();
        }

        public async Task<IReadOnlyList<Invoice>> GetOutstandingInvoicesForCustomerAsync(int customerId)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageInvoicesPayments);
            var invoices = await _unitOfWork.Invoices.GetByCustomerAsync(customerId);
            return invoices
                .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid)
                .ToList();
        }

        public async Task AdjustInvoiceAsync(int invoiceId, decimal otherCharges, decimal discountAmount, string reason, int? adjustedByUserId)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageInvoicesPayments);
            if (otherCharges < 0 || discountAmount < 0)
                throw new BusinessValidationException("Other charges and discount cannot be negative.");
            otherCharges = Math.Round(otherCharges, 2, MidpointRounding.AwayFromZero);
            discountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero);
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5 || reason.Trim().Length > 500)
                throw new BusinessValidationException("Enter a correction reason between 5 and 500 characters.");

            _unitOfWork.DetachAllTrackedEntities();
            await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var invoice = await _unitOfWork.Invoices.Query()
                    .Include(i => i.Customer)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId)
                    ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");
                if (invoice.Status == InvoiceStatus.Cancelled)
                    throw new BusinessValidationException("A cancelled invoice cannot be adjusted.");

                var discountBase = invoice.SubTotal + otherCharges;
                if (discountAmount > discountBase)
                    throw new BusinessValidationException("Discount cannot exceed the subtotal plus other charges.");

                var taxableAmount = invoice.SubTotal + otherCharges - discountAmount;
                var taxAmount = invoice.TaxApplied
                    ? Math.Round(taxableAmount * invoice.TaxPercentage / 100m, 2, MidpointRounding.AwayFromZero)
                    : 0m;
                var grandTotal = taxableAmount + taxAmount;
                if (grandTotal <= 0)
                    throw new BusinessValidationException("The corrected invoice total must remain greater than zero.");
                if (grandTotal < invoice.PaidAmount)
                    throw new BusinessValidationException($"The corrected total ({grandTotal:N2}) cannot be less than the amount already paid ({invoice.PaidAmount:N2}).");

                if (otherCharges == invoice.OtherCharges && discountAmount == invoice.DiscountAmount)
                    throw new BusinessValidationException("There are no invoice changes to save.");

                var oldTotal = invoice.GrandTotal;
                var delta = grandTotal - oldTotal;
                var adjustment = new InvoiceAdjustment
                {
                    Invoice = invoice,
                    AdjustmentDate = DateTime.Now,
                    PreviousOtherCharges = invoice.OtherCharges,
                    NewOtherCharges = otherCharges,
                    PreviousDiscountAmount = invoice.DiscountAmount,
                    NewDiscountAmount = discountAmount,
                    PreviousTaxAmount = invoice.TaxAmount,
                    NewTaxAmount = taxAmount,
                    PreviousGrandTotal = oldTotal,
                    NewGrandTotal = grandTotal,
                    Reason = reason.Trim(),
                    AdjustedByUserId = adjustedByUserId
                };
                await _unitOfWork.InvoiceAdjustments.AddAsync(adjustment);

                invoice.OtherCharges = otherCharges;
                invoice.DiscountAmount = discountAmount;
                invoice.TaxAmount = taxAmount;
                invoice.GrandTotal = grandTotal;
                invoice.Status = invoice.PaidAmount >= grandTotal
                    ? InvoiceStatus.Paid
                    : invoice.PaidAmount > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Unpaid;

                var customer = await _unitOfWork.Customers.GetByIdAsync(invoice.CustomerId)
                    ?? throw new KeyNotFoundException($"Customer {invoice.CustomerId} not found.");
                customer.CurrentBalance += delta;
                _unitOfWork.Customers.Update(customer);

                await _unitOfWork.LedgerEntries.AddAsync(new CustomerLedgerEntry
                {
                    CustomerId = customer.Id,
                    EntryDate = adjustment.AdjustmentDate,
                    Type = TransactionType.Adjustment,
                    Debit = delta > 0 ? delta : 0,
                    Credit = delta < 0 ? -delta : 0,
                    RunningBalance = customer.CurrentBalance,
                    InvoiceAdjustment = adjustment,
                    Notes = $"Invoice {invoice.InvoiceNumber} correction: {adjustment.Reason}"
                });

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Invoice {InvoiceNumber} corrected by {Delta} for reason: {Reason}", invoice.InvoiceNumber, delta, adjustment.Reason);
            }
            catch
            {
                await transaction.RollbackAsync();
                _unitOfWork.DetachAllTrackedEntities();
                throw;
            }
        }
    }
}
