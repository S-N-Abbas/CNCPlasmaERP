namespace CNCPlasmaERP.Core.Exceptions
{
    public sealed class AuthorizationException : DomainException
    {
        public AuthorizationException(string message) : base(message)
        {
        }
    }
}
