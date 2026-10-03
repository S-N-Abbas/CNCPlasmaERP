using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.UI.Models
{
    /// <summary>Bundles an Invoice with company settings for the printable A5 template, which needs both.</summary>
    public class InvoicePrintData
    {
        public Invoice Invoice { get; init; } = null!;
        public AppSettings Settings { get; init; } = null!;
    }
}
