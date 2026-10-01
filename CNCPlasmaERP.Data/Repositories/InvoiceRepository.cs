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
    public class InvoiceRepository : GenericRepository<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Invoice?> GetWithDetailsAsync(int invoiceId) =>
            await DbSet
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Job)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

        public async Task<IReadOnlyList<Invoice>> GetByCustomerAsync(int customerId) =>
            await DbSet.AsNoTracking()
                .Where(i => i.CustomerId == customerId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

        public async Task<IReadOnlyList<Invoice>> GetUnpaidOrPartialAsync() =>
            await DbSet.AsNoTracking()
                .Include(i => i.Customer)
                .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();

        public async Task<bool> InvoiceNumberExistsAsync(string invoiceNumber) =>
            await DbSet.AnyAsync(i => i.InvoiceNumber == invoiceNumber);
    }
}
