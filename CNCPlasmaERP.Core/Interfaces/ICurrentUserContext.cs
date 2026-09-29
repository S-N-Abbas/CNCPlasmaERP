using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Entities;

namespace CNCPlasmaERP.Core.Interfaces
{
    /// <summary>
    /// Holds the currently logged-in user for the lifetime of this run of the
    /// application. Registered as a Singleton in DI — a WPF desktop app has
    /// exactly one active session at a time, unlike a web app serving many
    /// concurrent users, so a single shared instance is the right lifetime here.
    /// </summary>
    public interface ICurrentUserContext
    {
        User? CurrentUser { get; }

        bool IsLoggedIn { get; }

        void SetCurrentUser(User user);

        /// <summary>Clears the session — call on logout.</summary>
        void Clear();

        bool HasPermission(Permission permission);
    }
}
