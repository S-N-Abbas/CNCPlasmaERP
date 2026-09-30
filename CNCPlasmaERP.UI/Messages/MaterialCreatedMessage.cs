using CNCPlasmaERP.Core.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CNCPlasmaERP.UI.Messages
{
    /// <summary>
    /// Broadcast whenever a new Material is created, so every open ViewModel that
    /// keeps its own copy of the materials list (Stock Purchases today, Job
    /// material entry later) can add it directly instead of needing a full reload
    /// or a direct reference to whichever ViewModel created it.
    /// </summary>
    public class MaterialCreatedMessage : ValueChangedMessage<Material>
    {
        public MaterialCreatedMessage(Material material) : base(material)
        {
        }
    }
}
