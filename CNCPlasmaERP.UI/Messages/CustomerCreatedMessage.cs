using CNCPlasmaERP.Core.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CNCPlasmaERP.UI.Messages
{
    /// <summary>Same reasoning as MaterialCreatedMessage — keeps any open customer dropdown in sync without a manual reload.</summary>
    public class CustomerCreatedMessage : ValueChangedMessage<Customer>
    {
        public CustomerCreatedMessage(Customer customer) : base(customer)
        {
        }
    }
}
