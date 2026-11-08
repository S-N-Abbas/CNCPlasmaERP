using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Exceptions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels.Base
{
    /// <summary>
    /// Shared base for every ViewModel: busy-state tracking and a standard
    /// pattern for running async work that separates expected business errors
    /// (DomainException — shown to the user as-is) from unexpected bugs (logged
    /// with full detail, shown to the user as a generic message).
    ///
    /// IMPORTANT: every async operation that touches the database MUST go
    /// through RunBusyAsync. All our Services share one long-lived DbContext for
    /// the session (needed so multi-service operations like InvoiceService ->
    /// LedgerService can share one transaction), and EF Core's DbContext is not
    /// safe for two operations to use concurrently — even when both happen on
    /// the UI thread via async/await, not separate hardware threads. DatabaseGate
    /// below serializes every RunBusyAsync call app-wide (across every open
    /// ViewModel) so two screens' fire-and-forget loads — e.g. two sibling
    /// ViewModels constructed back-to-back, each starting their own load in their
    /// constructor — can never overlap and corrupt the shared DbContext's state.
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject
    {
        private static readonly SemaphoreSlim DatabaseGate = new(1, 1);
        private static Action? _clearTrackedEntities;

        private readonly ILogger _logger;

        protected ViewModelBase(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Wires up a delegate that clears the shared session-wide DbContext's
        /// change tracker (IUnitOfWork.DetachAllTrackedEntities). Called once,
        /// from MainWindowViewModel's constructor — which already holds the
        /// IServiceProvider every feature ViewModel's services are ultimately
        /// resolved from — so every RunBusyAsync call after login starts with a
        /// clean slate, with no risk of one screen's stale tracked entity
        /// colliding with or corrupting another's operation. Before login this
        /// is unconfigured and RunBusyAsync simply skips the clear, which is
        /// fine: LoginViewModel's one simple operation carries none of the
        /// multi-entity tracking risk this guards against.
        /// </summary>
        public static void ConfigureDatabaseReset(Action clearTrackedEntities)
        {
            _clearTrackedEntities = clearTrackedEntities;
        }

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private string? statusMessage;

        /// <summary>
        /// Runs an async operation with busy-state tracking and centralized error
        /// handling. Re-entrant calls on THIS ViewModel while already busy are
        /// ignored (prevents a double-click from firing the same command twice).
        /// Separately, DatabaseGate queues this operation behind any other
        /// ViewModel's in-flight database work app-wide, so IsBusy is set
        /// immediately (the UI shows "busy" right away) even if the actual work
        /// has to wait its turn.
        /// </summary>
        protected async Task RunBusyAsync(Func<Task> operation, [CallerMemberName] string? callerName = null)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = null;
            StatusMessage = null;

            await DatabaseGate.WaitAsync();
            try
            {
                // Every top-level operation starts with a clean slate: no entity
                // left tracked from a PREVIOUS operation — especially one that
                // failed, since a transaction rollback at the database level
                // does NOT automatically undo EF Core's in-memory tracked
                // entity state — can collide with or corrupt this one.
                _clearTrackedEntities?.Invoke();

                await operation();
            }
            catch (DomainException ex)
            {
                // Expected business-rule violation — safe to show directly to the user.
                ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "Business rule violation in {Caller}", callerName);
            }
            catch (Exception ex)
            {
                // Unexpected — log full detail, show a generic message so nothing sensitive leaks.
                ErrorMessage = "An unexpected error occurred. Please try again or contact support.";
                _logger.LogError(ex, "Unexpected error in {Caller}", callerName);
            }
            finally
            {
                DatabaseGate.Release();
                IsBusy = false;
            }
        }
    }
}
