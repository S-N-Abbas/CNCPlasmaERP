using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class GenerateInvoiceViewModel : ViewModelBase
    {
        private readonly IJobService _jobService;
        private readonly IInvoiceService _invoiceService;
        private readonly ICurrentUserContext _currentUserContext;

        public GenerateInvoiceViewModel(
            IJobService jobService,
            IInvoiceService invoiceService,
            ICurrentUserContext currentUserContext,
            ILogger<GenerateInvoiceViewModel> logger)
            : base(logger)
        {
            _jobService = jobService;
            _invoiceService = invoiceService;
            _currentUserContext = currentUserContext;

            _ = LoadAsync();
        }

        public ObservableCollection<Job> CompletedJobs { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(InvoiceSubtotal))]
        [NotifyPropertyChangedFor(nameof(EstimatedTotal))]
        private Job? selectedJob;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EstimatedTotal))]
        private decimal otherCharges;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EstimatedTotal))]
        private decimal discountAmount;

        public decimal InvoiceSubtotal => SelectedJob is null ? 0m :
            SelectedJob.MaterialUsages.Sum(u => u.SellingAmount) +
            Math.Round(SelectedJob.ServiceCharges, 2, MidpointRounding.AwayFromZero) +
            Math.Round(SelectedJob.CarriageCharges, 2, MidpointRounding.AwayFromZero);

        public decimal EstimatedTotal => Math.Round(InvoiceSubtotal +
            Math.Round(OtherCharges, 2, MidpointRounding.AwayFromZero) -
            Math.Round(DiscountAmount, 2, MidpointRounding.AwayFromZero), 2, MidpointRounding.AwayFromZero);

        partial void OnSelectedJobChanged(Job? value)
        {
            OtherCharges = 0;
            DiscountAmount = 0;
        }

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadDataAsync);

        private async Task LoadDataAsync()
        {
            var selectedJobId = SelectedJob?.Id;
            var jobs = await _jobService.GetJobsByStatusAsync(JobStatus.Completed);
            CompletedJobs.Clear();
            foreach (var job in jobs)
            {
                CompletedJobs.Add(job);
            }

            SelectedJob = selectedJobId.HasValue
                ? CompletedJobs.FirstOrDefault(j => j.Id == selectedJobId.Value)
                : null;
        }

        [RelayCommand]
        private async Task GenerateInvoiceAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (SelectedJob is null)
                {
                    ErrorMessage = "Select a job to invoice.";
                    return;
                }
                if (OtherCharges < 0 || DiscountAmount < 0)
                {
                    ErrorMessage = "Other charges and discount cannot be negative.";
                    return;
                }
                if (DiscountAmount > InvoiceSubtotal + OtherCharges)
                {
                    ErrorMessage = "Discount cannot exceed the invoice subtotal plus other charges.";
                    return;
                }

                var jobNumber = SelectedJob.JobNumber;
                var invoice = await _invoiceService.CreateInvoiceFromJobAsync(SelectedJob.Id, _currentUserContext.CurrentUser?.Id, OtherCharges, DiscountAmount);
                StatusMessage = $"Invoice {invoice.InvoiceNumber} created for job {jobNumber}.";

                SelectedJob = null;

                // Raw helper, not RunBusyAsync-wrapped — safe to call here since we're already inside this method's RunBusyAsync.
                await LoadDataAsync();
            });
        }
    }
}
