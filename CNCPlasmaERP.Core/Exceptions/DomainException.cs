using System;

namespace CNCPlasmaERP.Core.Exceptions
{
    /// <summary>
    /// Base class for expected business-rule violations (insufficient stock,
    /// duplicate invoice number, etc). ViewModels should catch this type and show
    /// the message directly to the user, as opposed to an unexpected exception
    /// which indicates a bug and should be logged/surfaced generically instead.
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message)
        {
        }
    }
}
