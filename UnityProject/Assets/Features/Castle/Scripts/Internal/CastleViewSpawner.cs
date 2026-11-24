using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Castle
{
    /// <summary>Instantiates the castle view prefab from the config.</summary>
    internal sealed class CastleViewSpawner : ICastleViewSpawner
    {
        private readonly GameObject _prefab;

        public CastleViewSpawner(GameObject prefab)
        {
            _prefab = prefab ? prefab : throw new ArgumentNullException(nameof(prefab));
        }

        public ICastleView Spawn()
        {
            var instance = Object.Instantiate(_prefab);
            var view = instance.GetComponent<ICastleView>();
            if (view == null)
            {
                Object.Destroy(instance);
                throw new InvalidOperationException($"Castle view prefab '{_prefab.name}' has no component implementing {nameof(ICastleView)}.");
            }
            return view;
        }

        public void Despawn(ICastleView view)
        {
            if (view is Component component && component) Object.Destroy(component.gameObject);
        }
    }
}
