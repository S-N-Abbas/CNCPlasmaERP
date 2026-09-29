using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Builds a printable invoice from a completed Job: itemizes each material
    /// used plus service/carriage charges, applies tax per current AppSettings,
    /// marks the Job as Invoiced, and posts the debit to the customer's ledger —
    /// all as a single atomic operation.
    /// </summary>
    public interface IInvoiceService
    {
        Task<Invoice> CreateInvoiceFromJobAsync(int jobId, int? createdByUserId);

        /// <summary>Invoice with Customer, Job, Items and Payments loaded — for printing/detail screens.</summary>
        Task<Invoice?> GetInvoiceWithDetailsAsync(int invoiceId);
    }
}
