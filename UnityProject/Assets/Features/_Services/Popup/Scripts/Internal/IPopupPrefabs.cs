using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>Finds the prefab behind a popup id. Separated so the queue logic can be tested without assets.</summary>
    internal interface IPopupPrefabs
    {
        /// <summary>Throws, naming the id, when the scene has no prefab for it.</summary>
        GameObject Get(PopupId id);
    }
}
