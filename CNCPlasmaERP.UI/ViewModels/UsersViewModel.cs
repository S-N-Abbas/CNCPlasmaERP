using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Interfaces;
using CNCPlasmaERP.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CNCPlasmaERP.UI.ViewModels
{
    public partial class UsersViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserContext _currentUserContext;

        public UsersViewModel(IAuthService authService, ICurrentUserContext currentUserContext, ILogger<UsersViewModel> logger)
            : base(logger)
        {
            _authService = authService;
            _currentUserContext = currentUserContext;

            _ = LoadAsync();
        }

        public ObservableCollection<User> Users { get; } = new();

        public Array Roles => Enum.GetValues(typeof(UserRole));

        [ObservableProperty]
        private string newUsername = string.Empty;

        [ObservableProperty]
        private string newPassword = string.Empty;

        [ObservableProperty]
        private string newFullName = string.Empty;

        [ObservableProperty]
        private UserRole newRole = UserRole.Operator;

        [ObservableProperty]
        private User? selectedUser;

        [ObservableProperty]
        private string resetPassword = string.Empty;

        [RelayCommand]
        private async Task LoadAsync() => await RunBusyAsync(LoadDataAsync);

        private async Task LoadDataAsync()
        {
            var users = await _authService.GetAllUsersAsync();
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }

        [RelayCommand]
        private async Task CreateUserAsync()
        {
            await RunBusyAsync(async () =>
            {
                StatusMessage = null;

                if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(NewFullName))
                {
                    ErrorMessage = "Username, password and full name are all required.";
                    return;
                }

                if (NewPassword.Length < 12)
                {
                    ErrorMessage = "Password must be at least 12 characters.";
                    return;
                }

                await _authService.CreateUserAsync(NewUsername.Trim(), NewPassword, NewFullName.Trim(), NewRole);

                NewUsername = string.Empty;
                NewPassword = string.Empty;
                NewFullName = string.Empty;
                NewRole = UserRole.Operator;

                StatusMessage = "User created.";

                await LoadDataAsync();
            });
        }

        [RelayCommand]
        private async Task ToggleActiveAsync(User user)
        {
            await RunBusyAsync(async () =>
            {
                if (user.Id == _currentUserContext.CurrentUser?.Id)
                {
                    ErrorMessage = "You cannot deactivate your own account.";
                    return;
                }

                await _authService.SetUserActiveStatusAsync(user.Id, !user.IsActive);
                await LoadDataAsync();
            });
        }

        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            await RunBusyAsync(async () =>
            {
                StatusMessage = null;

                if (SelectedUser is null)
                {
                    ErrorMessage = "Select a user first.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(ResetPassword) || ResetPassword.Length < 12)
                {
                    ErrorMessage = "Enter a new password of at least 12 characters.";
                    return;
                }

                var username = SelectedUser.Username;
                await _authService.ResetPasswordAsync(SelectedUser.Id, ResetPassword);

                ResetPassword = string.Empty;
                StatusMessage = $"Password reset for '{username}'.";
            });
        }
    }
}
