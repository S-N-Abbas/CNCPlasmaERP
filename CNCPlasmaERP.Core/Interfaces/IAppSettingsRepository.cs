using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    public interface IAppSettingsRepository
    {
        /// <summary>Returns the single settings row, creating it with sensible defaults on first run if it doesn't exist yet.</summary>
        Task<AppSettings> GetSettingsAsync();

        Task SaveAsync(AppSettings settings);
    }
}
