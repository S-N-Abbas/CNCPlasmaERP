using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Enums;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace CNCPlasmaERP.Services
{
    /// <summary>
    /// Handles login and user/password management. Passwords are hashed with
    /// PBKDF2-SHA256 (100,000 iterations, random 128-bit salt per user) — never
    /// stored or compared in plain text.
    /// </summary>
    public class AuthService : IAuthService
    {
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;
        private const int Iterations = 100_000;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuthService> _logger;
        private readonly ICurrentUserContext _currentUserContext;

        public AuthService(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserContext = currentUserContext;
            _logger = logger;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user is null)
            {
                _logger.LogWarning("Login failed: username '{Username}' not found or inactive", username);
                return null;
            }

            if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Login failed: incorrect password for '{Username}'", username);
                return null;
            }

            user.LastLoginAt = DateTime.Now;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User '{Username}' logged in (Role: {Role})", username, user.Role);

            return user;
        }

        public async Task<User> CreateUserAsync(string username, string password, string fullName, UserRole role)
        {
            if (_currentUserContext.IsLoggedIn)
                ServiceAuthorization.Require(_currentUserContext, Permission.ManageUsers);
            else if (role != UserRole.Admin || (await _unitOfWork.Users.GetAllAsync()).Count != 0)
                throw new AuthorizationException("Only first-run administrator setup is allowed before login.");

            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length > 50 || string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > 150)
                throw new BusinessValidationException("Username and full name are required and must fit within the allowed length.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
                throw new BusinessValidationException("Password must be at least 12 characters.");
            username = username.Trim();
            fullName = fullName.Trim();
            var existing = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existing is not null)
            {
                throw new DuplicateUsernameException(username);
            }

            var (hash, salt) = HashPassword(password);

            var user = new User
            {
                Username = username,
                PasswordHash = hash,
                PasswordSalt = salt,
                FullName = fullName,
                Role = role,
                IsActive = true,
                MustChangePassword = _currentUserContext.IsLoggedIn
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User '{Username}' created with role {Role}", username, role);

            return user;
        }

        public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            if (_currentUserContext.CurrentUser?.Id != userId)
                throw new AuthorizationException("You may only change your own password.");
            ValidateNewPassword(newPassword);
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User {userId} not found.");

            if (!VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
            {
                throw new InvalidCredentialsException("Current password is incorrect.");
            }

            var (hash, salt) = HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.MustChangePassword = false;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User {UserId} changed their own password", userId);
        }

        public async Task ResetPasswordAsync(int userId, string newPassword)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageUsers);
            ValidateNewPassword(newPassword);
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User {userId} not found.");

            var (hash, salt) = HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.MustChangePassword = true;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogWarning(
                "Password for User {UserId} ('{Username}') was reset by an administrator", userId, user.Username);
        }

        public bool HasPermission(UserRole role, Permission permission) => RolePermissions.Has(role, permission);

        public async Task<IReadOnlyList<User>> GetAllUsersAsync()
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageUsers);
            var users = await _unitOfWork.Users.GetAllAsync();
            return users.Select(user => new User
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                MustChangePassword = user.MustChangePassword,
                LastLoginAt = user.LastLoginAt
            }).ToList();
        }

        public async Task SetUserActiveStatusAsync(int userId, bool isActive)
        {
            ServiceAuthorization.Require(_currentUserContext, Permission.ManageUsers);
            if (!isActive && _currentUserContext.CurrentUser?.Id == userId)
                throw new BusinessValidationException("You cannot deactivate your own account.");
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User {userId} not found.");

            if (!isActive && user.Role == UserRole.Admin && user.IsActive)
            {
                var anotherActiveAdmin = await _unitOfWork.Users.Query().AnyAsync(u => u.Id != userId && u.Role == UserRole.Admin && u.IsActive);
                if (!anotherActiveAdmin)
                    throw new BusinessValidationException("The last active administrator cannot be deactivated.");
            }

            user.IsActive = isActive;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User '{Username}' {Status}", user.Username, isActive ? "activated" : "deactivated");
        }

        private static (string HashBase64, string SaltBase64) HashPassword(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        private static void ValidateNewPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
                throw new BusinessValidationException("Password must be at least 12 characters.");
        }

        private static bool VerifyPassword(string password, string storedHashBase64, string storedSaltBase64)
        {
            var saltBytes = Convert.FromBase64String(storedSaltBase64);
            var computedHashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
            var storedHashBytes = Convert.FromBase64String(storedHashBase64);
            return CryptographicOperations.FixedTimeEquals(computedHashBytes, storedHashBytes);
        }
    }
}
