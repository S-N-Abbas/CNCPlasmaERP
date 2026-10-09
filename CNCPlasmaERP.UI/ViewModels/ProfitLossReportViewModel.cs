using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.Core.Reports;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class ProfitLossReportViewModel : ViewModelBase
    {
        private readonly IReportService _reportService;
        private readonly ISettingsService _settingsService;

        public ProfitLossReportViewModel(IReportService reportService, ISettingsService settingsService, ILogger<ProfitLossReportViewModel> logger)
            : base(logger)
        {
            _reportService = reportService;
            _settingsService = settingsService;

            fromDate = DateTime.Today.AddMonths(-1);
            toDate = DateTime.Today;

            _ = LoadAsync();
        }

        public ObservableCollection<ProfitByMaterialLine> MaterialLines { get; } = new();

        [ObservableProperty]
        private DateTime? fromDate;

        [ObservableProperty]
        private DateTime? toDate;

        [ObservableProperty]
        private ProfitLossSummary? summary;

        [ObservableProperty]
        private AppSettings? companySettings;

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadDataAsync);

        private async Task LoadDataAsync()
        {
            var from = FromDate ?? DateTime.Today.AddMonths(-1);
            var to = ToDate ?? DateTime.Today;

            var byMaterial = await _reportService.GetProfitByMaterialAsync(from, to);
            MaterialLines.Clear();
            foreach (var line in byMaterial.Lines)
            {
                MaterialLines.Add(line);
            }

            Summary = await _reportService.GetProfitLossSummaryAsync(from, to);
            CompanySettings ??= await _settingsService.GetSettingsAsync();
        }
    }
}
