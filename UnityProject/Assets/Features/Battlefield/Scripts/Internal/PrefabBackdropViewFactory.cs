using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Battlefield
{
    /// <summary>Makes the backdrop view from the view prefab and destroys it again.</summary>
    internal sealed class PrefabBackdropViewFactory : IBackdropViewFactory
    {
        private readonly GameObject _prefab;

        public PrefabBackdropViewFactory(GameObject prefab)
        {
            _prefab = prefab;
        }

        public IBackdropView Create()
        {
            var instance = Object.Instantiate(_prefab);
            instance.name = "Battlefield";
            var view = instance.GetComponent<IBackdropView>();
            if (view == null)
            {
                Object.Destroy(instance);
                throw new InvalidOperationException($"Battlefield view prefab '{_prefab.name}' has no backdrop view component");
            }

            return view;
        }

        public void Destroy(IBackdropView view)
        {
            if (view is Component component && component != null)
                Object.Destroy(component.gameObject);
        }
    }
}
