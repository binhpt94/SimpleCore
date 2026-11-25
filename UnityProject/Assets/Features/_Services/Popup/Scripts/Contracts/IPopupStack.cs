using System.Collections.Generic;

namespace Game.Services.Popup
{
    /// <summary>
    /// Read-only view of the popup stack, for checks such as "no popup left open when a new run starts".
    /// Same object as <see cref="IPopupService"/>.
    /// </summary>
    public interface IPopupStack
    {
        /// <summary>Ids of the popups open now, bottom of the stack first; empty when none is open.</summary>
        IReadOnlyList<PopupId> OpenIds { get; }
    }
}
