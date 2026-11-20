using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>What to show. The prefab belongs to the feature that asks; the service only instantiates it.</summary>
    public sealed class PopupRequest
    {
        public PopupRequest(GameObject prefab)
        {
            Prefab = prefab;
        }

        public GameObject Prefab { get; }
    }
}
