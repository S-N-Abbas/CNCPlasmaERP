using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    public interface ISupplierService
    {
        /// <summary>Active suppliers only, alphabetically — for dropdowns.</summary>
        Task<IReadOnlyList<Supplier>> GetActiveAsync();

        Task<Supplier> CreateAsync(Supplier supplier);

        Task<IReadOnlyList<Supplier>> GetAllAsync();

        Task UpdateAsync(Supplier supplier);
    }
}
