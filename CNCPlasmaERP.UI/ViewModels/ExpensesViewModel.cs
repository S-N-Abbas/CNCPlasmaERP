using CNCPlasmaERP.UI.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    /// <summary>Container for the Expenses screen's two tabs: general expenses, and wages.</summary>
    public partial class ExpensesViewModel : ViewModelBase
    {
        public ExpensesViewModel(ExpenseListViewModel expenseList, WagesViewModel wages, ILogger<ExpensesViewModel> logger)
            : base(logger)
        {
            ExpenseList = expenseList;
            Wages = wages;
        }

        public ExpenseListViewModel ExpenseList { get; }
        public WagesViewModel Wages { get; }
    }
}
