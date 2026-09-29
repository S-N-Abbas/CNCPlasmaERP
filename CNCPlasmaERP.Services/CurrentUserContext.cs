using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Entities;
using CNCPlasmaERP.Core.Interfaces;

namespace CNCPlasmaERP.Services
{
    /// <summary>
    /// Singleton holder for the logged-in user during this run of the application.
    /// Deliberately does not depend on IAuthService (a Scoped service) to avoid a
    /// captive-dependency lifetime mismatch — permission checks here go straight
    /// to the stateless RolePermissions lookup instead.
    /// </summary>
    public class CurrentUserContext : ICurrentUserContext
    {
        public User? CurrentUser { get; private set; }

        public bool IsLoggedIn => CurrentUser is not null;

        public void SetCurrentUser(User user) => CurrentUser = user;

        public void Clear() => CurrentUser = null;

        public bool HasPermission(Permission permission) =>
            CurrentUser is not null && RolePermissions.Has(CurrentUser.Role, permission);
    }
}
