using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Interfaces
{
    public interface IJobRepository : IGenericRepository<Job>
    {
        /// <summary>Job with Customer, MaterialUsages (+Material), Invoice and Quotation loaded — for detail/edit screens.</summary>
        Task<Job?> GetWithDetailsAsync(int jobId);

        /// <summary>Every job (any status), Customer loaded — for the unfiltered Jobs list.</summary>
        Task<IReadOnlyList<Job>> GetAllWithCustomerAsync();

        Task<IReadOnlyList<Job>> GetByCustomerAsync(int customerId);

        Task<IReadOnlyList<Job>> GetByStatusAsync(JobStatus status);

        Task<bool> JobNumberExistsAsync(string jobNumber);
    }
}
