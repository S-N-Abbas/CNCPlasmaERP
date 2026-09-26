using CNCPlasmaERP.Core.Common;

namespace CNCPlasmaERP.Core.Entities
{
    public class QuotationItem : BaseEntity
    {
        public int QuotationId { get; set; }
        public Quotation Quotation { get; set; } = null!;

        /// <summary>Optional — a quotation is just a record, so a material reference isn't mandatory.</summary>
        public int? MaterialId { get; set; }
        public Material? Material { get; set; }

        public string Description { get; set; } = string.Empty;
        public decimal? EstimatedWeightKg { get; set; }
        public decimal? EstimatedRatePerKg { get; set; }
        public decimal EstimatedAmount { get; set; }
    }
}
