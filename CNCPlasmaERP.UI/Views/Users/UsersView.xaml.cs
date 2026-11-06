using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using CNCPlasmaERP.UI.ViewModels;

namespace CNCPlasmaERP.UI.Views
{
    public partial class UsersView : UserControl
    {
        public UsersView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is UsersViewModel oldViewModel)
                oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            if (e.NewValue is UsersViewModel newViewModel)
                newViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not UsersViewModel viewModel)
                return;

            if (e.PropertyName == nameof(UsersViewModel.NewPassword) && NewPasswordBox.Password != viewModel.NewPassword)
                NewPasswordBox.Password = viewModel.NewPassword;
            else if (e.PropertyName == nameof(UsersViewModel.ResetPassword) && ResetPasswordBox.Password != viewModel.ResetPassword)
                ResetPasswordBox.Password = viewModel.ResetPassword;
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsersViewModel viewModel && viewModel.NewPassword != NewPasswordBox.Password)
                viewModel.NewPassword = NewPasswordBox.Password;
        }

        private void ResetPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsersViewModel viewModel && viewModel.ResetPassword != ResetPasswordBox.Password)
                viewModel.ResetPassword = ResetPasswordBox.Password;
        }
    }
}
