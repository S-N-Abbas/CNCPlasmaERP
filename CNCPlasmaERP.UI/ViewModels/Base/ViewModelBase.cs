using System;
using System.Runtime.CompilerServices;
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
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject
    {
        private readonly ILogger _logger;

        protected ViewModelBase(ILogger logger)
        {
            _logger = logger;
        }

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? errorMessage;

        /// <summary>
        /// Runs an async operation with busy-state tracking and centralized error
        /// handling. Re-entrant calls while already busy are ignored (prevents a
        /// double-click from firing the same command twice).
        /// </summary>
        protected async Task RunBusyAsync(Func<Task> operation, [CallerMemberName] string? callerName = null)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
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
                IsBusy = false;
            }
        }
    }
}
