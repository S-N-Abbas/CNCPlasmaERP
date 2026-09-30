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

            var allItems = new[]
            {
                new NavigationItem { Title = "Dashboard", Icon = "🏠", RequiredPermission = null, ViewModelType = typeof(DashboardViewModel) },
                new NavigationItem { Title = "Stock", Icon = "📦", RequiredPermission = Permission.ManageStock, ViewModelType = typeof(StockViewModel) }

                // More entries (Jobs, Customers, Suppliers, Invoicing,
                // Expenses, Wages, Reports, Settings, Users) are added here as
                // their ViewModels/Views are built in upcoming modules.
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

            // Resolving via DI here — rather than the usual constructor injection —
            // is the one deliberate exception to normal MVVM practice in this app.
            // The shell has to create arbitrary feature ViewModels on demand as the
            // user clicks around; that's exactly what a composition-root-adjacent
            // "shell" ViewModel is for. _serviceProvider is whichever scope this
            // ViewModel itself was resolved from (see the App.xaml.cs scope note),
            // so every screen the user navigates to shares one DbContext for the session.
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
