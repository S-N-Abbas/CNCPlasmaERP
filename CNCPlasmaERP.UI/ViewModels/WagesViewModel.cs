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

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class WagesViewModel : ViewModelBase
    {
        private readonly IExpenseService _expenseService;

        public WagesViewModel(IExpenseService expenseService, ILogger<WagesViewModel> logger)
            : base(logger)
        {
            _expenseService = expenseService;
            _ = LoadAsync();
        }

        public ObservableCollection<Expense> WagePayments { get; } = new();

        public Array PaymentMethods => Enum.GetValues(typeof(PaymentMethod));

        [ObservableProperty]
        private string workerName = string.Empty;

        [ObservableProperty]
        private decimal amount;

        [ObservableProperty]
        private PaymentMethod method = PaymentMethod.Cash;

        [ObservableProperty]
        private string? notes;

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadDataAsync);

        private async Task LoadDataAsync()
        {
            var wages = await _expenseService.GetByCategoryAsync(ExpenseCategory.Wages);
            WagePayments.Clear();
            foreach (var wage in wages)
            {
                WagePayments.Add(wage);
            }
        }

        [RelayCommand]
        private async Task AddWagePaymentAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(WorkerName))
                {
                    ErrorMessage = "Enter the worker's name.";
                    return;
                }

                if (Amount <= 0)
                {
                    ErrorMessage = "Amount must be greater than zero.";
                    return;
                }

                var expense = new Expense
                {
                    Category = ExpenseCategory.Wages,
                    Amount = Amount,
                    PaidTo = WorkerName.Trim(),
                    Description = Notes,
                    Method = Method
                };

                await _expenseService.CreateAsync(expense);
                WagePayments.Insert(0, expense);
                StatusMessage = "Wage payment recorded.";

                WorkerName = string.Empty;
                Amount = 0;
                Notes = null;
            });
        }
    }
}
