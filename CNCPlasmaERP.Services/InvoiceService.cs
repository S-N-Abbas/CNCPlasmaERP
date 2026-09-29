using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

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

        public InvoiceService(
            IUnitOfWork unitOfWork,
            ILedgerService ledgerService,
            INumberSequenceService numberSequenceService,
            ILogger<InvoiceService> logger)
        {
            _unitOfWork = unitOfWork;
            _ledgerService = ledgerService;
            _numberSequenceService = numberSequenceService;
            _logger = logger;
        }

        public async Task<Invoice> CreateInvoiceFromJobAsync(int jobId, int? createdByUserId)
        {
            var job = await _unitOfWork.Jobs.GetWithDetailsAsync(jobId)
                ?? throw new KeyNotFoundException($"Job {jobId} not found.");

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

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var items = BuildInvoiceItems(job);

                var subTotal = items.Sum(i => i.Amount);
                var taxApplied = settings.TaxEnabled;
                var taxPercentage = taxApplied ? settings.TaxPercentage : 0m;
                var taxAmount = Math.Round(subTotal * taxPercentage / 100m, 2);
                var grandTotal = subTotal + taxAmount;

                var invoiceNumber = await _numberSequenceService.GenerateInvoiceNumberAsync();

                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    JobId = job.Id,
                    CustomerId = job.CustomerId,
                    InvoiceDate = DateTime.Now,
                    SubTotal = subTotal,
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

                job.Status = JobStatus.Invoiced;
                _unitOfWork.Jobs.Update(job);

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
                    Amount = usage.SellingAmount
                });
            }

            if (job.ServiceCharges > 0)
            {
                items.Add(new InvoiceItem
                {
                    ItemType = InvoiceItemType.Service,
                    Description = "Cutting / Bending Service Charges",
                    Amount = job.ServiceCharges
                });
            }

            if (job.CarriageCharges > 0)
            {
                items.Add(new InvoiceItem
                {
                    ItemType = InvoiceItemType.Carriage,
                    Description = "Carriage / Delivery Charges",
                    Amount = job.CarriageCharges
                });
            }

            return items;
        }

        public Task<Invoice?> GetInvoiceWithDetailsAsync(int invoiceId) =>
            _unitOfWork.Invoices.GetWithDetailsAsync(invoiceId);
    }
}
