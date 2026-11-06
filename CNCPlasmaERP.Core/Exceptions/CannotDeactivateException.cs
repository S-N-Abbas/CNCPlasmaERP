namespace CNCPlasmaERP.Core.Exceptions
{
    /// <summary>
    /// Thrown when a record can't be deactivated yet — e.g. a material that
    /// still has stock, or a customer whose account balance isn't zero.
    /// Deactivated records disappear from every dropdown, so deactivating one
    /// that still holds stock or money would strand it where nothing could
    /// reach it. Shown to the user as-is.
    /// </summary>
    public class CannotDeactivateException : DomainException
    {
        public CannotDeactivateException(string message) : base(message)
        {
        }
    }
}
