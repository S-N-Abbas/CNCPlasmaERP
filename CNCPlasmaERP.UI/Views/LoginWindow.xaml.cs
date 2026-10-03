using System.Windows;
using CNCPlasmaERP.UI.ViewModels;
using MahApps.Metro.Controls;

namespace CNCPlasmaERP.UI.Views
{
    public partial class LoginWindow : MetroWindow
    {
        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;

            // WPF's PasswordBox doesn't support data binding on its Password
            // property (a deliberate security decision, to avoid plain-text
            // passwords sitting in the visual tree / bindable properties), so
            // this is the standard MVVM workaround: push the value across manually.
            PasswordInput.PasswordChanged += (_, _) => viewModel.Password = PasswordInput.Password;

            viewModel.LoginSucceeded += (_, _) =>
            {
                DialogResult = true;
                Close();
            };
        }
    }
}
