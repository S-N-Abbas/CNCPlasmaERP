namespace CNCPlasmaERP.Core.Exceptions
{
    public class DuplicateUsernameException : DomainException
    {
        public DuplicateUsernameException(string username)
            : base($"Username '{username}' is already taken.")
        {
        }
    }
}
