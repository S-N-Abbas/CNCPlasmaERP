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
                new NavigationItem { Section = "Overview", Title = "Dashboard", Icon = "\uE80F", RequiredPermission = null, ViewModelType = typeof(DashboardViewModel) },
                new NavigationItem { Section = "Work", Title = "Jobs", Icon = "\uE90F", RequiredPermission = Permission.ManageJobs, ViewModelType = typeof(JobsViewModel) },
                new NavigationItem { Section = "Work", Title = "Quotations", Icon = "\uE70F", RequiredPermission = Permission.ManageJobs, ViewModelType = typeof(QuotationsViewModel) },
                new NavigationItem { Section = "Sales", Title = "Invoicing", Icon = "\uE8A5", RequiredPermission = Permission.ManageInvoicesPayments, ViewModelType = typeof(InvoicingViewModel) },
                new NavigationItem { Section = "Sales", Title = "Customers", Icon = "\uE716", RequiredPermission = Permission.ManageCustomers, ViewModelType = typeof(CustomersViewModel) },
                new NavigationItem { Section = "Purchasing & Stock", Title = "Stock", Icon = "\uE7B8", RequiredPermission = Permission.ManageStock, ViewModelType = typeof(StockViewModel) },
                new NavigationItem { Section = "Purchasing & Stock", Title = "Suppliers", Icon = "\uE806", RequiredPermission = Permission.ManageSuppliers, ViewModelType = typeof(SuppliersViewModel) },
                new NavigationItem { Section = "Finance", Title = "Expenses", Icon = "\uE8C7", RequiredPermission = Permission.ManageExpenses, ViewModelType = typeof(ExpensesViewModel) },
                new NavigationItem { Section = "Finance", Title = "Reports", Icon = "\uE9F9", RequiredPermission = Permission.ViewFinancialReports, ViewModelType = typeof(ReportsViewModel) },
                new NavigationItem { Section = "Administration", Title = "Users", Icon = "\uE77B", RequiredPermission = Permission.ManageUsers, ViewModelType = typeof(UsersViewModel) },
                new NavigationItem { Section = "Administration", Title = "Settings", Icon = "\uE713", RequiredPermission = Permission.ManageSettings, ViewModelType = typeof(SettingsViewModel) },
            };

            var visibleItems = allItems.Where(i => i.RequiredPermission is null || _currentUserContext.HasPermission(i.RequiredPermission.Value)).ToList();
            NavigationItems = new ObservableCollection<NavigationItem>(visibleItems);
            NavigationSections = new ObservableCollection<NavigationSection>(visibleItems
                .GroupBy(i => i.Section)
                .Select(g => new NavigationSection { Title = g.Key, Items = new ObservableCollection<NavigationItem>(g) }));

            LoggedInUserDisplay = $"{currentUserContext.CurrentUser?.FullName} ({currentUserContext.CurrentUser?.Role})";

            if (NavigationItems.Count > 0)
            {
                NavigateTo(NavigationItems[0]);
            }
        }

        public ObservableCollection<NavigationItem> NavigationItems { get; }

        public ObservableCollection<NavigationSection> NavigationSections { get; }

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
