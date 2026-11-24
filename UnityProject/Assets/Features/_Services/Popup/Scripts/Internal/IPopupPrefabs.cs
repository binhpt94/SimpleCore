using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>Finds the prefab behind a popup id. Separated so the stack logic can be tested without assets.</summary>
    internal interface IPopupPrefabs
    {
        /// <summary>
        /// Prepares every popup of the scene. Throws, naming the id, when an id is not a popup id, is listed twice,
        /// or has no prefab. Called in the session's load step.
        /// </summary>
        void Prepare();

        /// <summary>False when the scene has no prefab for <paramref name="id"/>. Never called with the default id.</summary>
        bool TryGet(PopupId id, out GameObject prefab);
    }
}
