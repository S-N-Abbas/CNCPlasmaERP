using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IStockService _stockService;

        public DashboardViewModel(IStockService stockService, ICurrentUserContext currentUserContext, ILogger<DashboardViewModel> logger)
            : base(logger)
        {
            _stockService = stockService;

            WelcomeMessage = $"Welcome, {currentUserContext.CurrentUser?.FullName}";

            // Fire-and-forget is safe here: RunBusyAsync catches every exception
            // internally, so nothing can escape as an unobserved task exception.
            _ = LoadAsync();
        }

        public string WelcomeMessage { get; }

        public ObservableCollection<Material> LowStockMaterials { get; } = new();

        [RelayCommand]
        private async Task LoadAsync()
        {
            await RunBusyAsync(async () =>
            {
                var lowStock = await _stockService.GetLowStockMaterialsAsync();

                LowStockMaterials.Clear();
                foreach (var material in lowStock)
                {
                    LowStockMaterials.Add(material);
                }
            });
        }
    }
}
