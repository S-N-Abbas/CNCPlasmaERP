using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.Data;
using CNCPlasmaERP.Data.Context;
using CNCPlasmaERP.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.UI.ViewModels;
using CNCPlasmaERP.UI.Views;

namespace CNCPlasmaERP.UI
{
    /// <summary>
    /// Application entry point. Bootstraps a Generic Host so the whole app can use
    /// constructor-injected dependencies (DbContext, UnitOfWork, Services,
    /// ViewModels) instead of manually "new"-ing objects, and wires up Serilog as
    /// the logging backend behind the standard Microsoft.Extensions.Logging
    /// abstraction — Services/ViewModels only ever depend on ILogger&lt;T&gt;,
    /// never on Serilog directly.
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private IServiceScope? _mainScope;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Prevent WPF from shutting down when the LoginWindow closes before MainWindow opens
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var bootstrapConfiguration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(bootstrapConfiguration)
                .Enrich.FromLogContext()
                .CreateLogger();

            try
            {
                Log.Information("=== CNC Plasma ERP starting up ===");

                _host = Host.CreateDefaultBuilder()
                    .UseSerilog()
                    .ConfigureAppConfiguration(builder =>
                    {
                        builder.SetBasePath(AppContext.BaseDirectory);
                        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        ConfigureServices(context.Configuration, services);
                    })
                    .Build();

                // Start host background services if any exist
                await _host.StartAsync();

                RegisterGlobalExceptionHandlers();

                base.OnStartup(e);

                // Await asynchronously instead of GetAwaiter().GetResult()
                await SeedDefaultAdminUserAsync(_host.Services);

                using (var loginScope = _host.Services.CreateScope())
                {
                    var loginWindow = loginScope.ServiceProvider.GetRequiredService<LoginWindow>();
                    var loginResult = loginWindow.ShowDialog();

                    if (loginResult != true)
                    {
                        Shutdown();
                        return;
                    }
                }

                _mainScope = _host.Services.CreateScope();
                var mainWindow = _mainScope.ServiceProvider.GetRequiredService<MainWindow>();

                ShutdownMode = ShutdownMode.OnMainWindowClose;
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application failed to start");
                MessageBox.Show(
                    "The application failed to start. Please check the log file in the 'Logs' folder for details." +
                    Environment.NewLine + ex.Message,
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        /// <summary>
        /// Central DI registration. Add every new Service/Repository/ViewModel
        /// here as it's built, rather than scattering registrations elsewhere.
        /// </summary>
        private static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' was not found in appsettings.json.");

            services.AddDbContext<ApplicationDbContext>(
                options => options.UseSqlServer(connectionString),
                ServiceLifetime.Scoped);

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IStockService, StockService>();
            services.AddScoped<INumberSequenceService, NumberSequenceService>();

            services.AddScoped<ILedgerService, LedgerService>();
            services.AddScoped<IInvoiceService, InvoiceService>();

            services.AddScoped<IJobService, JobService>();


            services.AddScoped<IAuthService, AuthService>();
            services.AddSingleton<ICurrentUserContext, CurrentUserContext>();


            services.AddScoped<IMaterialService, MaterialService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<ICustomerService, CustomerService>();

            // TODO: register ViewModels and Views here as they are built
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<MaterialsViewModel>();
            services.AddTransient<StockPurchaseViewModel>();
            services.AddTransient<StockViewModel>();
            services.AddTransient<CustomerListViewModel>();
            services.AddTransient<CustomerLedgerViewModel>();
            services.AddTransient<CustomersViewModel>();
            services.AddTransient<SuppliersViewModel>();
        }

        /// <summary>
        /// Catches exceptions that would otherwise crash the app silently or with
        /// no record of what happened — every one of these is logged before the
        /// app decides whether it can continue or must shut down.
        /// </summary>
        private void RegisterGlobalExceptionHandlers()
        {
            // Exceptions raised on the WPF UI thread (by far the most common case).
            DispatcherUnhandledException += (sender, args) =>
            {
                Log.Error(args.Exception, "Unhandled exception on the UI thread");

                MessageBox.Show(
                    "An unexpected error occurred. It has been logged." +
                    Environment.NewLine + args.Exception.Message,
                    "Unexpected Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Keeps the app running instead of crashing where it's safe to do so.
                args.Handled = true;
            };

            // Exceptions raised on background threads outside the Dispatcher — these
            // are always fatal to the process, so we only get one chance to log them.
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Log.Fatal(
                    args.ExceptionObject as Exception,
                    "Unhandled exception on a background thread (terminating: {IsTerminating})",
                    args.IsTerminating);
            };

            // async void / fire-and-forget Tasks whose exceptions were never awaited.
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Log.Error(args.Exception, "Unobserved task exception");
                args.SetObserved();
            };
        }

        private static async Task SeedDefaultAdminUserAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            var anyUsers = (await unitOfWork.Users.GetAllAsync()).Count > 0;
            if (anyUsers)
            {
                return;
            }

            await authService.CreateUserAsync("admin", "Admin@123", "System Administrator", UserRole.Admin);

            Log.Warning("No users existed — created a default account (username: admin, password: Admin@123). Log in and change this password immediately.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("=== CNC Plasma ERP shutting down ===");
            Log.CloseAndFlush();
            _host?.Dispose();
            _mainScope?.Dispose();
            base.OnExit(e);
        }
    }
}
