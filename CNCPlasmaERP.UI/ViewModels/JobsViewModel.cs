using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Specialized;
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
    /// <summary>One entry in the status filter dropdown. Status = null means "All".</summary>
    public class JobStatusFilterItem
    {
        public string Label { get; init; } = string.Empty;
        public JobStatus? Status { get; init; }
    }

    public partial class JobsViewModel : ViewModelBase, IRecipient<CustomerCreatedMessage>, IRecipient<MaterialCreatedMessage>
    {
        private readonly IJobService _jobService;
        private readonly ICustomerService _customerService;
        private readonly IMaterialService _materialService;

        public JobsViewModel(
            IJobService jobService,
            ICustomerService customerService,
            IMaterialService materialService,
            ILogger<JobsViewModel> logger)
            : base(logger)
        {
            _jobService = jobService;
            _customerService = customerService;
            _materialService = materialService;

            SelectedJobUsages.CollectionChanged += OnSelectedJobUsagesChanged;

            WeakReferenceMessenger.Default.RegisterAll(this);

            selectedStatusFilter = StatusFilterOptions[0];

            _ = InitializeAsync();
        }

        public ObservableCollection<Job> Jobs { get; } = new();
        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<Material> Materials { get; } = new();
        public ObservableCollection<JobMaterialUsage> SelectedJobUsages { get; } = new();

        private void OnSelectedJobUsagesChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            OnPropertyChanged(nameof(CanCancelJob));

        public List<JobStatusFilterItem> StatusFilterOptions { get; } = new()
        {
            new() { Label = "All", Status = null },
            new() { Label = "Pending", Status = JobStatus.Pending },
            new() { Label = "In Progress", Status = JobStatus.InProgress },
            new() { Label = "Completed", Status = JobStatus.Completed },
            new() { Label = "Invoiced", Status = JobStatus.Invoiced },
            new() { Label = "Cancelled", Status = JobStatus.Cancelled }
        };

        [ObservableProperty]
        private JobStatusFilterItem selectedStatusFilter;

        partial void OnSelectedStatusFilterChanged(JobStatusFilterItem value) => _ = LoadJobsAsync();

        [ObservableProperty]
        private Job? selectedJob;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanMarkCompleted))]
        [NotifyPropertyChangedFor(nameof(CanCancelJob))]
        private Job? selectedJobDetail;

        public bool CanMarkCompleted => SelectedJobDetail?.Status == JobStatus.InProgress ||
            (SelectedJobDetail?.Status == JobStatus.Pending &&
             (SelectedJobDetail.ServiceCharges > 0 || SelectedJobDetail.CarriageCharges > 0));

        public bool CanCancelJob => SelectedJobDetail?.Status == JobStatus.Pending ||
            (SelectedJobDetail?.Status == JobStatus.InProgress && SelectedJobUsages.Count == 0);

        partial void OnSelectedJobChanged(Job? value) => _ = RunBusyAsync(LoadJobDetailDataAsync);

        // New Job form
        [ObservableProperty] private Customer? newJobCustomer;
        [ObservableProperty] private decimal newJobServiceCharges;
        [ObservableProperty] private decimal newJobCarriageCharges;
        [ObservableProperty] private string? newJobDescription;

        // Add Usage form
        [ObservableProperty] private Material? usageMaterial;
        [ObservableProperty] private decimal usageWeightKg;
        [ObservableProperty] private int? usageSheetsUsed;
        [ObservableProperty] private decimal usageSellingRatePerKg;
        [ObservableProperty] private decimal usageScrapGeneratedKg;
        [ObservableProperty] private string? usageNotes;

        private async Task InitializeAsync()
        {
            await RunBusyAsync(async () =>
            {
                var customers = await _customerService.GetActiveAsync();
                Customers.Clear();
                foreach (var customer in customers)
                {
                    Customers.Add(customer);
                }

                var materials = await _materialService.GetActiveAsync();
                Materials.Clear();
                foreach (var material in materials)
                {
                    Materials.Add(material);
                }

                await LoadJobsDataAsync();
            });
        }

        [RelayCommand]
        private async Task LoadJobsAsync() => await RunBusyAsync(LoadJobsDataAsync);

        /// <summary>Raw helper (not RunBusyAsync-wrapped) — see StockPurchaseViewModel for why this split matters.</summary>
        private async Task LoadJobsDataAsync()
        {
            var jobs = SelectedStatusFilter?.Status is null
                ? await _jobService.GetAllJobsAsync()
                : await _jobService.GetJobsByStatusAsync(SelectedStatusFilter.Status!.Value);

            Jobs.Clear();
            foreach (var job in jobs)
            {
                Jobs.Add(job);
            }
        }

        /// <summary>Raw helper — loads the full detail (with material usages) for SelectedJob.</summary>
        private async Task LoadJobDetailDataAsync()
        {
            SelectedJobUsages.Clear();

            if (SelectedJob is null)
            {
                SelectedJobDetail = null;
                return;
            }

            var job = await _jobService.GetJobWithDetailsAsync(SelectedJob.Id);
            SelectedJobDetail = job;

            if (job is not null)
            {
                foreach (var usage in job.MaterialUsages)
                {
                    SelectedJobUsages.Add(usage);
                }
            }
        }

        [RelayCommand]
        private async Task CreateJobAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (NewJobCustomer is null)
                {
                    ErrorMessage = "Select a customer.";
                    return;
                }

                var job = new Job
                {
                    CustomerId = NewJobCustomer.Id,
                    ServiceCharges = NewJobServiceCharges,
                    CarriageCharges = NewJobCarriageCharges,
                    Description = NewJobDescription
                };

                var created = await _jobService.CreateJobAsync(job);

                NewJobCustomer = null;
                NewJobServiceCharges = 0;
                NewJobCarriageCharges = 0;
                NewJobDescription = null;

                await LoadJobsDataAsync();

                // Set directly rather than relying on OnSelectedJobChanged's own
                // RunBusyAsync call, which would silently no-op here since we're
                // already inside this method's RunBusyAsync (IsBusy is still true).
                SelectedJob = Jobs.FirstOrDefault(j => j.Id == created.Id);
                await LoadJobDetailDataAsync();
            });
        }

        [RelayCommand]
        private async Task AddUsageAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (SelectedJob is null)
                {
                    ErrorMessage = "Select a job first.";
                    return;
                }

                if (UsageMaterial is null)
                {
                    ErrorMessage = "Select a material.";
                    return;
                }

                if (UsageWeightKg <= 0 || UsageSellingRatePerKg <= 0)
                {
                    ErrorMessage = "Weight and selling rate must be greater than zero.";
                    return;
                }

                await _jobService.AddMaterialUsageAsync(
                    SelectedJob.Id,
                    UsageMaterial.Id,
                    UsageWeightKg,
                    UsageSheetsUsed,
                    UsageSellingRatePerKg,
                    UsageScrapGeneratedKg,
                    UsageNotes);

                UsageMaterial = null;
                UsageWeightKg = 0;
                UsageSheetsUsed = null;
                UsageSellingRatePerKg = 0;
                UsageScrapGeneratedKg = 0;
                UsageNotes = null;

                await LoadJobDetailDataAsync();
                await LoadJobsDataAsync(); // job status may have flipped Pending -> InProgress
            });
        }

        [RelayCommand]
        private async Task RemoveUsageAsync(JobMaterialUsage usage)
        {
            await RunBusyAsync(async () =>
            {
                await _jobService.RemoveMaterialUsageAsync(usage.Id);
                await LoadJobDetailDataAsync();
            });
        }

        [RelayCommand]
        private async Task MarkCompletedAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (SelectedJob is null)
                {
                    return;
                }

                await _jobService.UpdateJobStatusAsync(SelectedJob.Id, JobStatus.Completed);
                await LoadJobsDataAsync();
                await LoadJobDetailDataAsync();
            });
        }

        [RelayCommand]
        private async Task CancelJobAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (SelectedJob is null)
                {
                    return;
                }

                await _jobService.UpdateJobStatusAsync(SelectedJob.Id, JobStatus.Cancelled);
                await LoadJobsDataAsync();
                await LoadJobDetailDataAsync();
            });
        }

        public void Receive(CustomerCreatedMessage message)
        {
            if (Customers.All(c => c.Id != message.Value.Id))
            {
                Customers.Add(message.Value);
            }
        }

        public void Receive(MaterialCreatedMessage message)
        {
            if (Materials.All(m => m.Id != message.Value.Id))
            {
                Materials.Add(message.Value);
            }
        }
    }
}
