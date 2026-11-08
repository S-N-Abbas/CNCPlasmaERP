using CommunityToolkit.Mvvm.ComponentModel;
using CNCPlasmaERP.UI.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    /// <summary>Container for the Customers screen's two tabs (list/add, and ledger/statement).</summary>
    public partial class CustomersViewModel : ViewModelBase
    {
        public CustomersViewModel(CustomerListViewModel customerListViewModel, CustomerLedgerViewModel ledgerViewModel, ILogger<CustomersViewModel> logger)
            : base(logger)
        {
            CustomerList = customerListViewModel;
            Ledger = ledgerViewModel;
        }

        public CustomerListViewModel CustomerList { get; }
        public CustomerLedgerViewModel Ledger { get; }

        [ObservableProperty]
        private int selectedTabIndex;

        partial void OnSelectedTabIndexChanged(int value)
        {
            if (value == 0)
            {
                CustomerList.LoadCommand.Execute(null);
            }
            else if (value == 1)
            {
                Ledger.LoadCustomersCommand.Execute(null);
            }
        }
    }
}
