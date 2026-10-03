using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    public interface ISettingsService
    {
        Task<AppSettings> GetSettingsAsync();

        Task SaveSettingsAsync(AppSettings settings);
    }
}
