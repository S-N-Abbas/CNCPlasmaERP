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
    public partial class ExpenseListViewModel : ViewModelBase
    {
        private readonly IExpenseService _expenseService;

        public ExpenseListViewModel(IExpenseService expenseService, ILogger<ExpenseListViewModel> logger)
            : base(logger)
        {
            _expenseService = expenseService;
            _ = LoadAsync();
        }

        public ObservableCollection<Expense> Expenses { get; } = new();

        /// <summary>Wages is deliberately excluded here — it has its own simpler tab with a "Worker Name" field.</summary>
        public ExpenseCategory[] Categories { get; } =
        {
            ExpenseCategory.Maintenance,
            ExpenseCategory.Electricity,
            ExpenseCategory.Rent,
            ExpenseCategory.Consumables,
            ExpenseCategory.Fuel,
            ExpenseCategory.Other
        };

        public Array PaymentMethods => Enum.GetValues(typeof(PaymentMethod));

        [ObservableProperty]
        private ExpenseCategory newCategory = ExpenseCategory.Maintenance;

        [ObservableProperty]
        private decimal newAmount;

        [ObservableProperty]
        private string? newPaidTo;

        [ObservableProperty]
        private string? newDescription;

        [ObservableProperty]
        private PaymentMethod newMethod = PaymentMethod.Cash;

        [ObservableProperty]
        private string? newReferenceNumber;

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadDataAsync);

        private async Task LoadDataAsync()
        {
            var all = await _expenseService.GetAllAsync();

            Expenses.Clear();
            foreach (var expense in all.Where(e => e.Category != ExpenseCategory.Wages))
            {
                Expenses.Add(expense);
            }
        }

        [RelayCommand]
        private async Task AddExpenseAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (NewAmount <= 0)
                {
                    ErrorMessage = "Amount must be greater than zero.";
                    return;
                }

                var expense = new Expense
                {
                    Category = NewCategory,
                    Amount = NewAmount,
                    PaidTo = NewPaidTo,
                    Description = NewDescription,
                    Method = NewMethod,
                    ReferenceNumber = NewReferenceNumber
                };

                await _expenseService.CreateAsync(expense);
                Expenses.Insert(0, expense);
                StatusMessage = "Expense recorded.";

                NewAmount = 0;
                NewPaidTo = null;
                NewDescription = null;
                NewReferenceNumber = null;
            });
        }
    }
}
