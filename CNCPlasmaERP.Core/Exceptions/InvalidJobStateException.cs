namespace CNCPlasmaERP.Core.Exceptions
{
    /// <summary>
    /// Thrown when an operation is attempted against a Job that's in the wrong
    /// state for it — e.g. trying to raise a second invoice for a job that's
    /// already been invoiced.
    /// </summary>
    public class InvalidJobStateException : DomainException
    {
        public InvalidJobStateException(string message) : base(message)
        {
        }
    }
}
