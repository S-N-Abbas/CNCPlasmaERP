namespace CNCPlasmaERP.Core.Exceptions
{
    /// <summary>
    /// Thrown when a quotation operation isn't allowed — e.g. changing a
    /// quotation that has already been converted to a job, or saving one with
    /// an expiry date in the past. Shown to the user as-is.
    /// </summary>
    public class InvalidQuotationException : DomainException
    {
        public InvalidQuotationException(string message) : base(message)
        {
        }
    }
}
