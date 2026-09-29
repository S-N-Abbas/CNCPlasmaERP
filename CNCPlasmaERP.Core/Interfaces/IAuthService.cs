using System.Threading.Tasks;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Login, user management and password handling. Passwords are never stored
    /// or compared in plain text — see AuthService for the PBKDF2 hashing used.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>Returns the authenticated user, or null if the username/password is wrong or the account is inactive.</summary>
        Task<User?> LoginAsync(string username, string password);

        Task<User> CreateUserAsync(string username, string password, string fullName, UserRole role);

        /// <summary>Self-service password change — requires knowing the current password.</summary>
        Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        /// <summary>Admin-triggered reset — does not require the old password.</summary>
        Task ResetPasswordAsync(int userId, string newPassword);

        bool HasPermission(UserRole role, Permission permission);
    }
}
