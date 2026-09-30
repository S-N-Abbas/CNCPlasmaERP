using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using CNCPlasmaERP.UI.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class MaterialsViewModel : ViewModelBase
    {
        private readonly IMaterialService _materialService;

        public MaterialsViewModel(IMaterialService materialService, ILogger<MaterialsViewModel> logger)
            : base(logger)
        {
            _materialService = materialService;
            _ = LoadAsync();
        }

        public ObservableCollection<Material> Materials { get; } = new();

        public Array MaterialTypes => Enum.GetValues(typeof(MaterialType));

        [ObservableProperty]
        private string newName = string.Empty;

        [ObservableProperty]
        private MaterialType newType = MaterialType.MS;

        [ObservableProperty]
        private string newGauge = string.Empty;

        [ObservableProperty]
        private decimal? newDefaultRatePerKg;

        [ObservableProperty]
        private decimal? newReorderLevelKg;

        [RelayCommand]
        private async Task LoadAsync()
        {
            await RunBusyAsync(async () =>
            {
                var materials = await _materialService.GetAllAsync();
                Materials.Clear();
                foreach (var material in materials)
                {
                    Materials.Add(material);
                }
            });
        }

        [RelayCommand]
        private async Task AddMaterialAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(NewName) || string.IsNullOrWhiteSpace(NewGauge))
                {
                    ErrorMessage = "Name and Gauge are required.";
                    return;
                }

                var material = new Material
                {
                    Name = NewName.Trim(),
                    Type = NewType,
                    Gauge = NewGauge.Trim(),
                    DefaultRatePerKg = NewDefaultRatePerKg,
                    ReorderLevelKg = NewReorderLevelKg,
                    IsActive = true
                };

                await _materialService.CreateAsync(material);
                Materials.Add(material);
                WeakReferenceMessenger.Default.Send(new MaterialCreatedMessage(material));

                NewName = string.Empty;
                NewGauge = string.Empty;
                NewDefaultRatePerKg = null;
                NewReorderLevelKg = null;
            });
        }
    }
}
