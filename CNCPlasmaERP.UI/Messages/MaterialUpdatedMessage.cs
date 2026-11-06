using CNCPlasmaERP.Core.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CNCPlasmaERP.UI.Messages
{
    /// <summary>
    /// Broadcast after a Material is edited or activated/deactivated. Any open
    /// ViewModel holding its own materials list (e.g. the Purchases tab's
    /// dropdown) swaps in the new copy, or drops it if IsActive is now false.
    /// </summary>
    public class MaterialUpdatedMessage : ValueChangedMessage<Material>
    {
        public MaterialUpdatedMessage(Material material) : base(material)
        {
        }
    }
}
