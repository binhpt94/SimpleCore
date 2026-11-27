using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.CheatPanel
{
    /// <summary>Instantiates the cheat overlay prefab in the current scene.</summary>
    internal sealed class PrefabCheatOverlaySpawner : ICheatOverlaySpawner
    {
        internal const string MissingPrefabMessage =
            "CheatPanel: cheat_enabled is on but the installer has no overlay prefab (overlayPrefab).";

        internal const string MissingDisplayMessage =
            "CheatPanel: the overlay prefab (overlayPrefab) has no cheat panel view (ICheatPanelDisplay).";

        private readonly GameObject _prefab;

        public PrefabCheatOverlaySpawner(GameObject prefab)
        {
            _prefab = prefab ? prefab : throw new InvalidOperationException(MissingPrefabMessage);
            if (_prefab.GetComponent<ICheatPanelDisplay>() == null)
                throw new InvalidOperationException(MissingDisplayMessage);
        }

        public GameObject Spawn(CheatPanelConfig config)
        {
            var instance = Object.Instantiate(_prefab);
            instance.name = _prefab.name;
            if (config != null) instance.GetComponent<ICheatPanelDisplay>()?.Show(FirstScreen(config));
            return instance;
        }

        /// <summary>Closed overlay drawn with the config's texts (button_label, panel_title, normal_row_label, empty_label) before the rules attach.</summary>
        internal static CheatPanelScreen FirstScreen(CheatPanelConfig config)
        {
            var rows = new[] { new CheatRow(config.NormalRowLabel, null, null) };
            return new CheatPanelScreen(config.ButtonLabel, false, config.PanelTitle, rows, 0, false, config.EmptyLabel);
        }

        public void Despawn(GameObject overlay)
        {
            if (overlay != null) Object.Destroy(overlay);
        }
    }
}
