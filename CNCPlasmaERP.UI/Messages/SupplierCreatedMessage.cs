using CNCPlasmaERP.Core.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CNCPlasmaERP.UI.Messages
{
    /// <summary>
    /// Broadcast whenever a new Supplier is created — same reasoning as
    /// MaterialCreatedMessage. Prevents the same staleness bug from reappearing
    /// once a standalone Suppliers management screen exists.
    /// </summary>
    public class SupplierCreatedMessage : ValueChangedMessage<Supplier>
    {
        public SupplierCreatedMessage(Supplier supplier) : base(supplier)
        {
        }
    }
}
