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

        private readonly GameObject _prefab;

        public PrefabCheatOverlaySpawner(GameObject prefab)
        {
            _prefab = prefab ? prefab : throw new InvalidOperationException(MissingPrefabMessage);
        }

        public GameObject Spawn(CheatPanelConfig config)
        {
            var instance = Object.Instantiate(_prefab);
            instance.name = _prefab.name;
            return instance;
        }

        public void Despawn(GameObject overlay)
        {
            if (overlay != null) Object.Destroy(overlay);
        }
    }
}
