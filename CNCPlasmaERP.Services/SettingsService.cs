using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(IUnitOfWork unitOfWork, ILogger<SettingsService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public Task<AppSettings> GetSettingsAsync() => _unitOfWork.Settings.GetSettingsAsync();

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            await _unitOfWork.Settings.SaveAsync(settings);
            _logger.LogInformation("Application settings updated");
        }
    }
}
