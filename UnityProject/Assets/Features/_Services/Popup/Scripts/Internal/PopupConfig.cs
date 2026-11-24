using System;
using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>
    /// Tuning of the popup layer (feature-popup/popup-layer/stack-rules). The backdrop darkness lives in the
    /// backdrop prefab's colour; the layer's draw order above the HUD lives in the layer prefab's canvas.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Config/Popup")]
    internal sealed class PopupConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int maxOpen = 3;
        [Tooltip("Screen-space canvas that holds every popup of the scene, drawn above all other views.")]
        [SerializeField] private GameObject layerPrefab;
        [Tooltip("Full-screen dark image that takes every touch, placed right behind each popup.")]
        [SerializeField] private GameObject backdropPrefab;
        // Always serialized so editor and player share one layout; read only in dev builds (feature-popup/cheat).
        [Tooltip("Dev only: the service's own sample popup Popup/DevSample (empty panel, one Close button).")]
        [SerializeField] private GameObject devSamplePrefab;

        internal int MaxOpen => maxOpen;
        internal GameObject LayerPrefab => layerPrefab;
        internal GameObject BackdropPrefab => backdropPrefab;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal GameObject DevSamplePrefab => devSamplePrefab;
#endif

        /// <summary>Throws, naming the missing field, when the config cannot drive the layer.</summary>
        internal void Check()
        {
            if (maxOpen < 1) throw new InvalidOperationException($"Popup config '{name}': maxOpen must be at least 1.");
            if (layerPrefab == null) throw new InvalidOperationException($"Popup config '{name}': layerPrefab is missing.");
            if (backdropPrefab == null) throw new InvalidOperationException($"Popup config '{name}': backdropPrefab is missing.");
        }
    }
}
