using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    public interface IInvoiceRepository : IGenericRepository<Invoice>
    {
        /// <summary>Invoice with Customer, Job, Items and Payments loaded — for printing/detail screens.</summary>
        Task<Invoice?> GetWithDetailsAsync(int invoiceId);

        Task<IReadOnlyList<Invoice>> GetByCustomerAsync(int customerId);

        /// <summary>Every invoice, Customer loaded — for the unfiltered Invoices list.</summary>
        Task<IReadOnlyList<Invoice>> GetAllWithCustomerAsync();

        /// <summary>Invoices with an outstanding balance — feeds the "Receivables" dashboard widget.</summary>
        Task<IReadOnlyList<Invoice>> GetUnpaidOrPartialAsync();

        Task<bool> InvoiceNumberExistsAsync(string invoiceNumber);
    }
}
