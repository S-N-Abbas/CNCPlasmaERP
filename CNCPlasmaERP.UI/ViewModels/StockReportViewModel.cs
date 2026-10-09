using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class StockReportViewModel : ViewModelBase
    {
        private readonly IReportService _reportService;
        private readonly ISettingsService _settingsService;

        public StockReportViewModel(IReportService reportService, ISettingsService settingsService, ILogger<StockReportViewModel> logger)
            : base(logger)
        {
            _reportService = reportService;
            _settingsService = settingsService;

            _ = LoadAsync();
        }

        public ObservableCollection<Material> Materials { get; } = new();

        [ObservableProperty]
        private AppSettings? companySettings;

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadDataAsync);

        private async Task LoadDataAsync()
        {
            var materials = await _reportService.GetStockReportAsync();
            Materials.Clear();
            foreach (var material in materials)
            {
                Materials.Add(material);
            }

            CompanySettings ??= await _settingsService.GetSettingsAsync();
        }
    }
}
