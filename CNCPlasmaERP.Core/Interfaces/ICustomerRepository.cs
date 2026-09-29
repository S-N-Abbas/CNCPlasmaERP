using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        /// <summary>Customer with ledger history loaded (newest first) — for the running-account statement screen.</summary>
        Task<Customer?> GetWithLedgerAsync(int customerId);

        Task<IReadOnlyList<Customer>> GetActiveAsync();

        /// <summary>Customers currently owing money — feeds the "Receivables" dashboard widget.</summary>
        Task<IReadOnlyList<Customer>> GetWithOutstandingBalanceAsync();
    }
}
