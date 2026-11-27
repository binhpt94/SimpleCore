using System;
using Game.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Castle
{
    /// <summary>Instantiates the castle view prefab from the config and hands it the art.</summary>
    internal sealed class CastleViewSpawner : ICastleViewSpawner
    {
        private readonly GameObject _prefab;
        private readonly IArt _art;

        public CastleViewSpawner(GameObject prefab, IArt art)
        {
            _prefab = prefab ? prefab : throw new ArgumentNullException(nameof(prefab));
            _art = art ?? throw new ArgumentNullException(nameof(art));
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
            view.UseArt(_art);
            return view;
        }

        public void Despawn(ICastleView view)
        {
            if (view is Component component && component) Object.Destroy(component.gameObject);
        }
    }
}
