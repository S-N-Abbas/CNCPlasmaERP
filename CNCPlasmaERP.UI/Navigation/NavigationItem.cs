using System;
using CNCPlasmaERP.Core.Authorization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CNCPlasmaERP.UI.Navigation
{
    /// <summary>
    /// Describes one entry in the shell's side navigation. RequiredPermission is
    /// nullable — null means "always visible regardless of role" (e.g. Dashboard);
    /// everything else is filtered against the logged-in user's permissions.
    /// </summary>
    public partial class NavigationItem : ObservableObject
    {
        public string Title { get; init; } = string.Empty;

        /// <summary>Simple text glyph for now — swap for a proper icon set later if desired.</summary>
        public string Icon { get; init; } = string.Empty;

        public Permission? RequiredPermission { get; init; }

        public Type ViewModelType { get; init; } = typeof(object);

        [ObservableProperty]
        private bool isSelected;
    }
}
