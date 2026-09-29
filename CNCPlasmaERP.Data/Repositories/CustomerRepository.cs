using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CNCPlasmaERP.Data.Repositories
{
    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Customer?> GetWithLedgerAsync(int customerId) =>
            await DbSet
                .Include(c => c.LedgerEntries.OrderByDescending(l => l.EntryDate))
                .FirstOrDefaultAsync(c => c.Id == customerId);

        public async Task<IReadOnlyList<Customer>> GetActiveAsync() =>
            await DbSet.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

        public async Task<IReadOnlyList<Customer>> GetWithOutstandingBalanceAsync() =>
            await DbSet.AsNoTracking()
                .Where(c => c.CurrentBalance > 0)
                .OrderByDescending(c => c.CurrentBalance)
                .ToListAsync();
    }
}
