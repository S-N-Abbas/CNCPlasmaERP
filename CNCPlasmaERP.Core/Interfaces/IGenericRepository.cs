using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Basic CRUD + querying contract shared by every entity. Business-specific
    /// query methods that need eager-loading or joins across aggregates live on
    /// the specialized repository interfaces (IJobRepository, IInvoiceRepository,
    /// ICustomerRepository) instead of being bolted on here.
    /// </summary>
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>Tracked lookup by primary key — use when the caller intends to modify the entity.</summary>
        Task<T?> GetByIdAsync(int id);

        /// <summary>Read-only (no change tracking) — use for populating lists/grids.</summary>
        Task<IReadOnlyList<T>> GetAllAsync();

        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        /// <summary>Escape hatch for callers (specialized repositories) that need Include()/ordering/paging.</summary>
        IQueryable<T> Query();

        Task AddAsync(T entity);

        void Update(T entity);

        void Remove(T entity);
    }
}
