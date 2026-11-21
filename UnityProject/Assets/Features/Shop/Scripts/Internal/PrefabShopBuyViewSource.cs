using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Shop
{
    /// <summary>Makes the buy view from the shop's own prefab.</summary>
    internal sealed class PrefabShopBuyViewSource : IShopBuyViewSource
    {
        private readonly GameObject prefab;
        private GameObject instance;

        internal PrefabShopBuyViewSource(GameObject prefab)
        {
            this.prefab = prefab;
        }

        /// <summary>True when the prefab's root carries a buy view. The installer checks this up front.</summary>
        internal static bool HasView(GameObject prefab)
        {
            return prefab.GetComponent<IShopBuyView>() != null;
        }

        public IShopBuyView Open()
        {
            if (instance != null) throw new InvalidOperationException("The shop buy view is already open.");
            instance = Object.Instantiate(prefab);
            return instance.GetComponent<IShopBuyView>();
        }

        public void Close()
        {
            if (instance == null) return;
            Object.Destroy(instance);
            instance = null;
        }
    }
}
