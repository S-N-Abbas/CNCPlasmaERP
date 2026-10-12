using System.Collections.Generic;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Read access to scrap balances and sales history, plus recording a sale.
    /// Recording delegates to IStockService, which stays the only code that
    /// ever changes a Scrap balance.
    /// </summary>
    public interface IScrapService
    {
        /// <summary>Current unsold scrap per material (Material loaded), alphabetically.</summary>
        Task<IReadOnlyList<Scrap>> GetBalancesAsync();

        /// <summary>Most recent scrap sales (Material loaded), newest first.</summary>
        Task<IReadOnlyList<ScrapSale>> GetRecentSalesAsync(int count = 50);

        /// <summary>Throws InsufficientScrapException if the material's scrap balance is too low.</summary>
        Task RecordSaleAsync(ScrapSale sale);
    }
}
