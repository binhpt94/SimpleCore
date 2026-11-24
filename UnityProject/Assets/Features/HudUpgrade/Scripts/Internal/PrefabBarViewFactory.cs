using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.HudUpgrade
{
    /// <summary>Instantiates the bar prefab; its root carries the component that implements <see cref="IUpgradeBarView"/>.</summary>
    internal sealed class PrefabBarViewFactory : IUpgradeBarViewFactory
    {
        private readonly GameObject _prefab;

        public PrefabBarViewFactory(GameObject prefab)
        {
            _prefab = prefab ? prefab : throw new ArgumentNullException(nameof(prefab));
        }

        public IUpgradeBarView Create()
        {
            var instance = Object.Instantiate(_prefab);
            instance.name = _prefab.name;
            var view = instance.GetComponent<IUpgradeBarView>();
            if (view == null)
            {
                Object.Destroy(instance);
                throw new InvalidOperationException($"HudUpgrade: prefab '{_prefab.name}' has no IUpgradeBarView component.");
            }

            return view;
        }

        public void Destroy(IUpgradeBarView view)
        {
            if (view is Component component && component) Object.Destroy(component.gameObject);
        }
    }
}
