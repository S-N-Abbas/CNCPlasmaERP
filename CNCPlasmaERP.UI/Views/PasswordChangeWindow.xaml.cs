using System;
using System.Windows;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
using MahApps.Metro.Controls;

namespace CNCPlasmaERP.UI.Views
{
    public partial class PasswordChangeWindow : MetroWindow
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserContext _currentUserContext;

        public PasswordChangeWindow(IAuthService authService, ICurrentUserContext currentUserContext)
        {
            InitializeComponent();
            _authService = authService;
            _currentUserContext = currentUserContext;
        }

        private async void Change_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = string.Empty;
            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                ErrorText.Text = "New passwords do not match.";
                return;
            }

            var user = _currentUserContext.CurrentUser;
            if (user is null)
            {
                ErrorText.Text = "The user session has ended. Restart and sign in again.";
                return;
            }

            try
            {
                await _authService.ChangePasswordAsync(user.Id, CurrentPasswordBox.Password, NewPasswordBox.Password);
                user.MustChangePassword = false;
                DialogResult = true;
            }
            catch (DomainException ex)
            {
                ErrorText.Text = ex.Message;
            }
            catch (Exception)
            {
                ErrorText.Text = "Password change failed. Check the application log and try again.";
            }
        }
    }
}
