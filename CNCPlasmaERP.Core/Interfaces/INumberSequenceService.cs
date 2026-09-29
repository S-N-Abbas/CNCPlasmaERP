using System.Threading.Tasks;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Generates sequential, human-readable document numbers (e.g. "INV-2026-0001").
    /// The sequence resets to 1 automatically at the start of each calendar year.
    /// </summary>
    public interface INumberSequenceService
    {
        Task<string> GenerateJobNumberAsync();
        Task<string> GenerateInvoiceNumberAsync();
        Task<string> GenerateQuotationNumberAsync();
    }
}
