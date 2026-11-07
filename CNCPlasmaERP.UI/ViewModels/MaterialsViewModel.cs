using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.Messages;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

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

        /// <summary>The grid row being edited. Null means the form is in "add new" mode.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditing))]
        [NotifyPropertyChangedFor(nameof(FormTitle))]
        [NotifyPropertyChangedFor(nameof(FormButtonText))]
        private Material? selectedMaterial;

        public bool IsEditing => SelectedMaterial is not null;
        public string FormTitle => IsEditing ? "Edit Material" : "Add Material";
        public string FormButtonText => IsEditing ? "Save Changes" : "Add Material";

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

        [ObservableProperty]
        private bool newIsActive = true;

        partial void OnSelectedMaterialChanged(Material? value)
        {
            ErrorMessage = null;

            if (value is null)
            {
                ResetForm();
                return;
            }

            NewName = value.Name;
            NewType = value.Type;
            NewGauge = value.Gauge;
            NewDefaultRatePerKg = value.DefaultRatePerKg;
            NewReorderLevelKg = value.ReorderLevelKg;
            NewIsActive = value.IsActive;
        }

        private void ResetForm()
        {
            NewName = string.Empty;
            NewType = MaterialType.MS;
            NewGauge = string.Empty;
            NewDefaultRatePerKg = null;
            NewReorderLevelKg = null;
            NewIsActive = true;
        }

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadDataAsync);

        /// <summary>Raw helper (not RunBusyAsync-wrapped) so SaveMaterialAsync can reuse it after its own RunBusyAsync has started.</summary>
        private async Task LoadDataAsync()
        {
            var materials = await _materialService.GetAllAsync();
            Materials.Clear();
            foreach (var material in materials)
            {
                Materials.Add(material);
            }
        }

        [RelayCommand]
        private void CancelEdit()
        {
            SelectedMaterial = null; // leaving edit mode also resets the form
        }

        [RelayCommand]
        private void EditMaterial(Material material)
        {
            SelectedMaterial = material;
        }

        [RelayCommand]
        private async Task SaveMaterialAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(NewName) || string.IsNullOrWhiteSpace(NewGauge))
                {
                    ErrorMessage = "Name and Gauge are required.";
                    return;
                }

                var editingId = SelectedMaterial?.Id;

                // A detached object carrying only the form's values. The service
                // copies the editable fields onto the stored record; stock
                // quantities are never part of an edit.
                var material = new Material
                {
                    Id = editingId ?? 0,
                    Name = NewName.Trim(),
                    Type = NewType,
                    Gauge = NewGauge.Trim(),
                    DefaultRatePerKg = NewDefaultRatePerKg,
                    ReorderLevelKg = NewReorderLevelKg,
                    IsActive = editingId.HasValue ? NewIsActive : true
                };

                int savedId;
                if (editingId.HasValue)
                {
                    await _materialService.UpdateAsync(material);
                    savedId = editingId.Value;
                }
                else
                {
                    var created = await _materialService.CreateAsync(material);
                    savedId = created.Id;
                }

                // Leave edit mode first (resets the form), then reload the grid.
                SelectedMaterial = null;
                await LoadDataAsync();

                // Broadcast the freshly loaded copy (not the tracked instance).
                var fresh = Materials.FirstOrDefault(m => m.Id == savedId);
                if (fresh is not null)
                {
                    if (editingId.HasValue)
                    {
                        WeakReferenceMessenger.Default.Send(new MaterialUpdatedMessage(fresh));
                    }
                    else
                    {
                        WeakReferenceMessenger.Default.Send(new MaterialCreatedMessage(fresh));
                    }
                }
            });
        }
    }
}
