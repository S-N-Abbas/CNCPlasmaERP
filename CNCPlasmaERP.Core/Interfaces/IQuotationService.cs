using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Quotations are record-only estimates: nothing here ever touches stock or
    /// the customer ledger. Turning one into a real Job is done through
    /// IJobService.ConvertQuotationToJobAsync.
    /// </summary>
    public interface IQuotationService
    {
        /// <summary>All quotations (Customer and Items loaded), newest first.</summary>
        Task<IReadOnlyList<Quotation>> GetAllAsync();

        /// <summary>One quotation with Customer, Items (+Material) and the converted Job loaded.</summary>
        Task<Quotation?> GetWithDetailsAsync(int quotationId);

        /// <summary>Generates the quotation number and saves the quotation with its items as a Draft.</summary>
        Task<Quotation> CreateAsync(Quotation quotation, IEnumerable<QuotationItem> items);

        /// <summary>Marks a Draft quotation as Sent. Not allowed once it has been converted to a job.</summary>
        Task MarkAsSentAsync(int quotationId);
    }
}
