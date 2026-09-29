using CNCPlasmaERP.Core.Common;

namespace CNCPlasmaERP.Core.Entities
{
    /// <summary>
    /// Running scrap-inventory balance per material. One row per Material.
    /// Increased when a Job records leftover offcut (JobMaterialUsage.ScrapGeneratedKg);
    /// decreased when scrap is sold (ScrapSale).
    /// </summary>
    public class Scrap : BaseEntity
    {
        public int MaterialId { get; set; }
        public Material Material { get; set; } = null!;

        /// <summary>Accumulated unsold scrap weight in Kg.</summary>
        public decimal BalanceKg { get; set; }

    }
}
