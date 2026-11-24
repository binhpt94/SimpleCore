using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Hero
{
    /// <summary>Instantiates the hero prefab under one "Heroes" root; the prefab's root carries an <see cref="IHeroDisplay"/>.</summary>
    internal sealed class PrefabHeroDisplayFactory : IHeroDisplayFactory
    {
        private readonly GameObject _prefab;
        private GameObject _root;

        public PrefabHeroDisplayFactory(GameObject prefab)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
        }

        public IHeroDisplay Create()
        {
            if (_root == null) _root = new GameObject("Heroes");
            var instance = Object.Instantiate(_prefab, _root.transform);
            var display = instance.GetComponent<IHeroDisplay>();
            if (display != null) return display;
            Object.Destroy(instance);
            throw new InvalidOperationException($"Hero prefab '{_prefab.name}' has no hero view on its root.");
        }

        public void RemoveAll()
        {
            if (_root == null) return;
            Object.Destroy(_root);
            _root = null;
        }
    }
}
