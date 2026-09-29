using System;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserContext _currentUserContext;

        public LoginViewModel(IAuthService authService, ICurrentUserContext currentUserContext, ILogger<LoginViewModel> logger)
            : base(logger)
        {
            _authService = authService;
            _currentUserContext = currentUserContext;
        }

        [ObservableProperty]
        private string username = string.Empty;

        /// <summary>
        /// Set from code-behind on PasswordBox.PasswordChanged — WPF's PasswordBox
        /// deliberately doesn't support data binding to its Password property for
        /// security reasons, so this is the standard, pragmatic MVVM workaround.
        /// </summary>
        [ObservableProperty]
        private string password = string.Empty;

        /// <summary>Raised when login succeeds so the hosting window can close itself and let App.xaml.cs proceed to the main shell.</summary>
        public event EventHandler? LoginSucceeded;

        [RelayCommand]
        private async Task LoginAsync()
        {
            await RunBusyAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "Please enter both username and password.";
                    return;
                }

                var user = await _authService.LoginAsync(Username.Trim(), Password);

                if (user is null)
                {
                    ErrorMessage = "Invalid username or password.";
                    return;
                }

                _currentUserContext.SetCurrentUser(user);
                Password = string.Empty;

                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
