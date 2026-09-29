namespace CNCPlasmaERP.Core.Exceptions
{
    /// <summary>
    /// Thrown when a job material usage can't be cleanly reversed — most commonly
    /// because the scrap it accrued has already been partially or fully sold, so
    /// it can no longer be subtracted back out of the scrap balance.
    /// </summary>
    public class CannotReverseUsageException : DomainException
    {
        public CannotReverseUsageException(string message) : base(message)
        {
        }
    }
}
