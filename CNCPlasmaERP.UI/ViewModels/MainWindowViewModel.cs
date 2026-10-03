using System;
using System.Collections.ObjectModel;
using System.Linq;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.Navigation;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    /// <summary>
    /// The application shell: builds the side navigation from the items the
    /// logged-in user actually has permission to see, and swaps the content area
    /// between feature ViewModels as the user navigates.
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICurrentUserContext _currentUserContext;

        public MainWindowViewModel(
            IServiceProvider serviceProvider,
            ICurrentUserContext currentUserContext,
            ILogger<MainWindowViewModel> logger)
            : base(logger)
        {
            _serviceProvider = serviceProvider;
            _currentUserContext = currentUserContext;

            // Wires every ViewModel's RunBusyAsync (via the static hook on
            // ViewModelBase) to clear the shared session-wide DbContext's
            // change tracker before each operation — see ViewModelBase and
            // IUnitOfWork.DetachAllTrackedEntities for the full reasoning.
            // Resolved here (rather than via constructor injection into
            // ViewModelBase, which would ripple into every derived ViewModel's
            // constructor) because this is the one place that already holds the
            // exact IServiceProvider every feature ViewModel's services share.
            var unitOfWorkForReset = _serviceProvider.GetRequiredService<IUnitOfWork>();
            ConfigureDatabaseReset(unitOfWorkForReset.DetachAllTrackedEntities);

            var allItems = new[]
            {
                new NavigationItem { Title = "Dashboard", Icon = "🏠", RequiredPermission = null, ViewModelType = typeof(DashboardViewModel) },
                new NavigationItem { Title = "Stock", Icon = "📦", RequiredPermission = Permission.ManageStock, ViewModelType = typeof(StockViewModel) },
                new NavigationItem { Title = "Customers", Icon = "👥", RequiredPermission = Permission.ManageCustomers, ViewModelType = typeof(CustomersViewModel) },
                new NavigationItem { Title = "Suppliers", Icon = "🚚", RequiredPermission = Permission.ManageSuppliers, ViewModelType = typeof(SuppliersViewModel) },
                new NavigationItem { Title = "Jobs", Icon = "🛠️", RequiredPermission = Permission.ManageJobs, ViewModelType = typeof(JobsViewModel) },
                new NavigationItem { Title = "Invoicing", Icon = "🧾", RequiredPermission = Permission.ManageInvoicesPayments, ViewModelType = typeof(InvoicingViewModel) },
                new NavigationItem { Title = "Expenses", Icon = "💵", RequiredPermission = Permission.ManageExpenses, ViewModelType = typeof(ExpensesViewModel) },
                new NavigationItem { Title = "Settings", Icon = "⚙️", RequiredPermission = Permission.ManageSettings, ViewModelType = typeof(SettingsViewModel) }

                // More entries (Reports, Users) are added here as their
                // ViewModels/Views are built in upcoming modules.
            };

            NavigationItems = new ObservableCollection<NavigationItem>(
                allItems.Where(i => i.RequiredPermission is null || _currentUserContext.HasPermission(i.RequiredPermission.Value)));

            LoggedInUserDisplay = $"{currentUserContext.CurrentUser?.FullName} ({currentUserContext.CurrentUser?.Role})";

            if (NavigationItems.Count > 0)
            {
                NavigateTo(NavigationItems[0]);
            }
        }

        public ObservableCollection<NavigationItem> NavigationItems { get; }

        public string LoggedInUserDisplay { get; }

        [ObservableProperty]
        private ViewModelBase? currentViewModel;

        [ObservableProperty]
        private NavigationItem? selectedNavigationItem;

        [RelayCommand]
        private void NavigateTo(NavigationItem item)
        {
            SelectedNavigationItem = item;

            // Update selection state on all sidebar navigation items
            foreach (var navItem in NavigationItems)
            {
                navItem.IsSelected = (navItem == item);
            }

            CurrentViewModel = (ViewModelBase)_serviceProvider.GetRequiredService(item.ViewModelType);
        }

        [RelayCommand]
        private void Logout()
        {
            _currentUserContext.Clear();
            System.Windows.Application.Current.Shutdown();
            // A nicer future improvement is to re-show LoginWindow and a fresh
            // MainWindow instead of exiting entirely — kept simple for now.
        }
    }
}
