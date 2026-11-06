using System;
using System.Windows;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using MahApps.Metro.Controls;

namespace CNCPlasmaERP.UI.Views
{
    public partial class InitialAdminWindow : MetroWindow
    {
        private readonly IAuthService _authService;

        public InitialAdminWindow(IAuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = string.Empty;
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                ErrorText.Text = "Passwords do not match.";
                return;
            }

            try
            {
                await _authService.CreateUserAsync(UsernameBox.Text.Trim(), PasswordBox.Password, FullNameBox.Text.Trim(), UserRole.Admin);
                DialogResult = true;
            }
            catch (DomainException ex)
            {
                ErrorText.Text = ex.Message;
            }
            catch (Exception)
            {
                ErrorText.Text = "Administrator setup failed. Check the application log and try again.";
            }
        }
    }
}
