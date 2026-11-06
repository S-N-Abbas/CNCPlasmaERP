using CNCPlasmaERP.Core.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CNCPlasmaERP.UI.Messages
{
    /// <summary>
    /// Broadcast after a Customer is edited or activated/deactivated. The
    /// Ledger tab's customer dropdown listens for this.
    /// </summary>
    public class CustomerUpdatedMessage : ValueChangedMessage<Customer>
    {
        public CustomerUpdatedMessage(Customer customer) : base(customer)
        {
        }
    }
}
