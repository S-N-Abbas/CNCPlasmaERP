using CNCPlasmaERP.Core.Authorization;
using CNCPlasmaERP.Core.Exceptions;
using CNCPlasmaERP.Core.Interfaces;

namespace CNCPlasmaERP.Services
{
    internal static class ServiceAuthorization
    {
        public static void Require(ICurrentUserContext context, Permission permission)
        {
            if (!context.HasPermission(permission))
            {
                throw new AuthorizationException($"You do not have permission to perform this action ({permission}).");
            }
        }
    }
}
