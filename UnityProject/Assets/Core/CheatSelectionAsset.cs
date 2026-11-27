#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Asset that keeps the one <see cref="CheatSelection"/> of the game process, so the pick lives through scene
    /// loads. The selection sits in a field that is not serialized: nothing is saved into the asset or to the device.
    /// The asset is kept loaded (<see cref="HideFlags.DontUnloadUnusedAsset"/>) so a scene load does not drop it.
    /// In the Editor, entering Play mode starts from an empty selection. No static, no singleton: Boot of every scene
    /// references this asset (_Composition) and registers <see cref="Selection"/>. Dev builds only.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Core/Cheat Selection", fileName = "CheatSelection")]
    public sealed class CheatSelectionAsset : ScriptableObject
    {
        [System.NonSerialized] private CheatSelection selection;

        /// <summary>The shared selection. Never null.</summary>
        public ICheatSelection Selection => selection ??= new CheatSelection();

        private void OnEnable()
        {
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
            selection = new CheatSelection();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        // Covers Enter Play Mode without domain reload, where OnEnable is not called again.
        private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.ExitingEditMode)
                selection = new CheatSelection();
        }
#endif
    }
}
#endif
