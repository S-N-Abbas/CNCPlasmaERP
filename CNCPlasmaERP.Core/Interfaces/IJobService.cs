using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Orchestrates the Job lifecycle: creation, adding/removing material usage
    /// (delegating all stock movement to IStockService), status transitions, and
    /// converting a Quotation into a real Job.
    /// </summary>
    public interface IJobService
    {
        Task<Job> CreateJobAsync(Job job);

        /// <summary>Consumes material for a job via IStockService and flips the job to InProgress if it was still Pending.</summary>
        Task<JobMaterialUsage> AddMaterialUsageAsync(
            int jobId,
            int materialId,
            decimal weightUsedKg,
            int? sheetsUsed,
            decimal sellingRatePerKg,
            decimal scrapGeneratedKg,
            string? notes);

        /// <summary>Reverses a material usage line (restores stock/scrap). Blocked once the job has been invoiced.</summary>
        Task RemoveMaterialUsageAsync(int jobMaterialUsageId);

        Task UpdateJobStatusAsync(int jobId, JobStatus newStatus);

        Task<Job?> GetJobWithDetailsAsync(int jobId);

        /// <summary>Every job (any status), Customer loaded — for the unfiltered Jobs list.</summary>
        Task<IReadOnlyList<Job>> GetAllJobsAsync();

        Task<IReadOnlyList<Job>> GetJobsByStatusAsync(JobStatus status);

        /// <summary>Creates a real Job from a Quotation's estimated charges, linking the two records together.</summary>
        Task<Job> ConvertQuotationToJobAsync(int quotationId);
    }
}
