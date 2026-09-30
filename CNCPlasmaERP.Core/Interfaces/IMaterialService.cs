using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    public interface IMaterialService
    {
        Task<IReadOnlyList<Material>> GetAllAsync();

        /// <summary>Active materials only, alphabetically — for dropdowns (job entry, stock purchase).</summary>
        Task<IReadOnlyList<Material>> GetActiveAsync();

        /// <summary>Throws DuplicateMaterialException if a material with the same Name+Gauge+Type already exists.</summary>
        Task<Material> CreateAsync(Material material);

        Task UpdateAsync(Material material);
    }
}
