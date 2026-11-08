using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;

        public SettingsViewModel(ISettingsService settingsService, ILogger<SettingsViewModel> logger)
            : base(logger)
        {
            _settingsService = settingsService;
            _ = LoadAsync();
        }

        [ObservableProperty]
        private AppSettings? settings;

        [RelayCommand]
        private async Task LoadAsync()
        {
            await RunBusyAsync(async () =>
            {
                Settings = await _settingsService.GetSettingsAsync();
            });
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            await RunBusyAsync(async () =>
            {
                StatusMessage = null;

                if (Settings is null)
                {
                    return;
                }

                if (Settings.TaxEnabled && (Settings.TaxPercentage < 0 || Settings.TaxPercentage > 100))
                {
                    ErrorMessage = "Tax percentage must be between 0 and 100.";
                    return;
                }

                await _settingsService.SaveSettingsAsync(Settings);

                StatusMessage = "Settings saved successfully.";
            });
        }
    }
}
