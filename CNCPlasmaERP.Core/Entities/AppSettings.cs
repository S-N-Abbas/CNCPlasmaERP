using CNCPlasmaERP.Core.Common;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// Single-row configuration table (always Id = 1). Holds company info for
    /// printouts and business-wide toggles like tax and document numbering.
    /// </summary>
    public class AppSettings : BaseEntity
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyAddress { get; set; }
        public string? CompanyPhone { get; set; }
        public string? CompanyEmail { get; set; }
        public string? LogoPath { get; set; }

        public bool TaxEnabled { get; set; }
        public decimal TaxPercentage { get; set; }

        public string CurrencySymbol { get; set; } = "Rs.";

        public string InvoicePrefix { get; set; } = "INV";
        public int InvoiceStartingNumber { get; set; } = 1;
        public int CurrentInvoiceSequence { get; set; } = 0;

        public string QuotationPrefix { get; set; } = "QUO";
        public int CurrentQuotationSequence { get; set; } = 0;

        public string JobPrefix { get; set; } = "JOB";
        public int CurrentJobSequence { get; set; } = 0;

        public int InvoiceSequenceYear { get; set; }
        public int JobSequenceYear { get; set; }
        public int QuotationSequenceYear { get; set; }
    }
}
