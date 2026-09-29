using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CNCPlasmaERP.Data.Repositories
{
    public class JobRepository : GenericRepository<Job>, IJobRepository
    {
        public JobRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Job?> GetWithDetailsAsync(int jobId) =>
            await DbSet
                .Include(j => j.Customer)
                .Include(j => j.MaterialUsages).ThenInclude(u => u.Material)
                .Include(j => j.Invoice)
                .Include(j => j.Quotation)
                .FirstOrDefaultAsync(j => j.Id == jobId);

        public async Task<IReadOnlyList<Job>> GetByCustomerAsync(int customerId) =>
            await DbSet.AsNoTracking()
                .Include(j => j.Customer)
                .Where(j => j.CustomerId == customerId)
                .OrderByDescending(j => j.JobDate)
                .ToListAsync();

        public async Task<IReadOnlyList<Job>> GetByStatusAsync(JobStatus status) =>
            await DbSet.AsNoTracking()
                .Include(j => j.Customer)
                .Where(j => j.Status == status)
                .OrderByDescending(j => j.JobDate)
                .ToListAsync();

        public async Task<bool> JobNumberExistsAsync(string jobNumber) =>
            await DbSet.AnyAsync(j => j.JobNumber == jobNumber);
    }
}
