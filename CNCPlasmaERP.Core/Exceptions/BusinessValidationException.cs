namespace CNCPlasmaERP.Core.Exceptions
{
    public sealed class BusinessValidationException : DomainException
    {
        public BusinessValidationException(string message) : base(message)
        {
        }
    }
}
