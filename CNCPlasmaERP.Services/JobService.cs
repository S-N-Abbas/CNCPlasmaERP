using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    /// <summary>
    /// Orchestrates the Job lifecycle. Never mutates stock or scrap directly —
    /// every material movement is delegated to IStockService, which remains the
    /// single source of truth for those numbers.
    /// </summary>
    public class JobService : IJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStockService _stockService;
        private readonly INumberSequenceService _numberSequenceService;
        private readonly ILogger<JobService> _logger;

        public JobService(
            IUnitOfWork unitOfWork,
            IStockService stockService,
            INumberSequenceService numberSequenceService,
            ILogger<JobService> logger)
        {
            _unitOfWork = unitOfWork;
            _stockService = stockService;
            _numberSequenceService = numberSequenceService;
            _logger = logger;
        }

        public async Task<Job> CreateJobAsync(Job job)
        {
            job.JobNumber = await _numberSequenceService.GenerateJobNumberAsync();
            job.Status = JobStatus.Pending;

            await _unitOfWork.Jobs.AddAsync(job);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Job {JobNumber} created for Customer {CustomerId}", job.JobNumber, job.CustomerId);

            return job;
        }

        public async Task<JobMaterialUsage> AddMaterialUsageAsync(
            int jobId,
            int materialId,
            decimal weightUsedKg,
            int? sheetsUsed,
            decimal sellingRatePerKg,
            decimal scrapGeneratedKg,
            string? notes)
        {
            var job = await _unitOfWork.Jobs.GetByIdAsync(jobId)
                ?? throw new KeyNotFoundException($"Job {jobId} not found.");

            if (job.Status is JobStatus.Invoiced or JobStatus.Cancelled)
            {
                throw new InvalidJobStateException(
                    $"Cannot add material to Job {job.JobNumber} — it is already {job.Status}.");
            }

            var usage = await _stockService.RecordJobMaterialUsageAsync(
                jobId, materialId, weightUsedKg, sheetsUsed, sellingRatePerKg, scrapGeneratedKg, notes);

            if (job.Status == JobStatus.Pending)
            {
                job.Status = JobStatus.InProgress;
                _unitOfWork.Jobs.Update(job);
                await _unitOfWork.SaveChangesAsync();
            }

            return usage;
        }

        public async Task RemoveMaterialUsageAsync(int jobMaterialUsageId)
        {
            var usage = await _unitOfWork.JobMaterialUsages.GetByIdAsync(jobMaterialUsageId)
                ?? throw new KeyNotFoundException($"Job material usage {jobMaterialUsageId} not found.");

            var job = await _unitOfWork.Jobs.GetByIdAsync(usage.JobId)
                ?? throw new KeyNotFoundException($"Job {usage.JobId} not found.");

            if (job.Status == JobStatus.Invoiced)
            {
                throw new InvalidJobStateException(
                    $"Cannot modify material usage on Job {job.JobNumber} — it has already been invoiced.");
            }

            await _stockService.ReverseJobMaterialUsageAsync(jobMaterialUsageId);

            _logger.LogInformation("Material usage {UsageId} removed from Job {JobNumber}", jobMaterialUsageId, job.JobNumber);
        }

        public async Task UpdateJobStatusAsync(int jobId, JobStatus newStatus)
        {
            var job = await _unitOfWork.Jobs.GetByIdAsync(jobId)
                ?? throw new KeyNotFoundException($"Job {jobId} not found.");

            if (job.Status == JobStatus.Invoiced && newStatus != JobStatus.Invoiced)
            {
                throw new InvalidJobStateException(
                    $"Cannot change the status of Job {job.JobNumber} — it has already been invoiced.");
            }

            job.Status = newStatus;
            _unitOfWork.Jobs.Update(job);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Job {JobNumber} status changed to {Status}", job.JobNumber, newStatus);
        }

        public Task<Job?> GetJobWithDetailsAsync(int jobId) => _unitOfWork.Jobs.GetWithDetailsAsync(jobId);

        public Task<IReadOnlyList<Job>> GetAllJobsAsync() => _unitOfWork.Jobs.GetAllWithCustomerAsync();

        public Task<IReadOnlyList<Job>> GetJobsByStatusAsync(JobStatus status) => _unitOfWork.Jobs.GetByStatusAsync(status);

        public async Task<Job> ConvertQuotationToJobAsync(int quotationId)
        {
            var quotation = await _unitOfWork.Quotations.Query()
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == quotationId)
                ?? throw new KeyNotFoundException($"Quotation {quotationId} not found.");

            if (quotation.ConvertedToJobId.HasValue)
            {
                throw new InvalidJobStateException(
                    $"Quotation {quotation.QuotationNumber} has already been converted to a job.");
            }

            var job = new Job
            {
                CustomerId = quotation.CustomerId,
                JobDate = DateTime.Now,
                ServiceCharges = quotation.EstimatedServiceCharges,
                CarriageCharges = quotation.EstimatedCarriageCharges,
                Description = $"Converted from Quotation {quotation.QuotationNumber}",
                QuotationId = quotation.Id,
                JobNumber = await _numberSequenceService.GenerateJobNumberAsync(),
                Status = JobStatus.Pending
            };

            await _unitOfWork.Jobs.AddAsync(job);

            // Set via navigation (not quotation.ConvertedToJobId = job.Id, since job.Id
            // doesn't exist yet) so EF Core's relationship fixup resolves the FK once
            // Job's Id is generated — both inserts commit together in one SaveChanges call.
            quotation.ConvertedToJob = job;
            quotation.Status = QuotationStatus.Converted;
            _unitOfWork.Quotations.Update(quotation);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Quotation {QuotationNumber} converted to Job {JobNumber}",
                quotation.QuotationNumber, job.JobNumber);

            return job;
        }
    }
}
