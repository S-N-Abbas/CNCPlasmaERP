using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SettingsService> _logger;
        private readonly ICurrentUserContext _currentUserContext;

        public SettingsService(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext, ILogger<SettingsService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUserContext = currentUserContext;
        }

        public Task<AppSettings> GetSettingsAsync()
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageSettings);
            return _unitOfWork.Settings.GetSettingsAsync();
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageSettings);
            if (settings.TaxPercentage < 0 || settings.TaxPercentage > 100)
                throw new BusinessValidationException("Tax percentage must be between 0 and 100.");
            if (string.IsNullOrWhiteSpace(settings.InvoicePrefix) || string.IsNullOrWhiteSpace(settings.JobPrefix) || string.IsNullOrWhiteSpace(settings.QuotationPrefix))
                throw new BusinessValidationException("Document number prefixes cannot be empty.");
            await _unitOfWork.Settings.SaveAsync(settings);
            _logger.LogInformation("Application settings updated");
        }
    }
}
