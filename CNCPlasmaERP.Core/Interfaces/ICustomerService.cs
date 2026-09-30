using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    public interface ICustomerService
    {
        Task<IReadOnlyList<Customer>> GetAllAsync();

        /// <summary>Active customers only, alphabetically — for dropdowns (Job creation, Invoicing).</summary>
        Task<IReadOnlyList<Customer>> GetActiveAsync();

        /// <summary>Creates the customer and, if OpeningBalance is non-zero, posts it to the ledger atomically.</summary>
        Task<Customer> CreateAsync(Customer customer);

        Task UpdateAsync(Customer customer);
    }
}
