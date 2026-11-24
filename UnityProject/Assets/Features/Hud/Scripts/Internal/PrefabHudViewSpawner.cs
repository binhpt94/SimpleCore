using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Hud
{
    /// <summary>Creates the HUD view from its prefab. The prefab's root must carry a component implementing <see cref="IHudView"/>.</summary>
    internal sealed class PrefabHudViewSpawner : IHudViewSpawner
    {
        private readonly GameObject _prefab;

        public PrefabHudViewSpawner(GameObject prefab)
        {
            _prefab = prefab ? prefab : throw new ArgumentNullException(nameof(prefab));
        }

        public IHudView Spawn()
        {
            var instance = Object.Instantiate(_prefab);
            instance.name = _prefab.name;
            var view = instance.GetComponent<IHudView>();
            if (view == null)
            {
                Object.Destroy(instance);
                throw new InvalidOperationException($"Hud: prefab '{_prefab.name}' has no IHudView component on its root.");
            }

            return view;
        }
    }
}
