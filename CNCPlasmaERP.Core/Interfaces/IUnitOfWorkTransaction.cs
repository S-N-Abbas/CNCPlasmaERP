using System;
using System.Threading.Tasks;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Thin abstraction over a database transaction so the Core and Services
    /// layers stay persistence-technology agnostic (no direct reference to
    /// Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction).
    /// </summary>
    public interface IUnitOfWorkTransaction : IAsyncDisposable
    {
        Task CommitAsync();
        Task RollbackAsync();
    }
}
