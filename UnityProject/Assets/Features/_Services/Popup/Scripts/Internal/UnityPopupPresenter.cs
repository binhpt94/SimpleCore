using System.Collections.Generic;
using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>
    /// Instantiates popups under one layer canvas (made from the layer prefab on the first popup). Children of the
    /// layer are drawn in sibling order, so "backdrop, popup" pairs stack up: the newest popup's backdrop covers
    /// and blocks everything below it, older popups included. The layer belongs to the scene; it is not kept
    /// across scene loads.
    /// </summary>
    internal sealed class UnityPopupPresenter : IPopupPresenter
    {
        private readonly GameObject layerPrefab;
        private readonly GameObject backdropPrefab;
        private readonly Dictionary<GameObject, GameObject> backdrops = new Dictionary<GameObject, GameObject>();
        private GameObject layer;

        internal UnityPopupPresenter(GameObject layerPrefab, GameObject backdropPrefab)
        {
            this.layerPrefab = layerPrefab;
            this.backdropPrefab = backdropPrefab;
        }

        public GameObject Layer => layer;

        public GameObject Present(GameObject prefab)
        {
            if (layer == null)
            {
                layer = Object.Instantiate(layerPrefab);
                layer.name = layerPrefab.name;
            }

            var backdrop = Object.Instantiate(backdropPrefab, layer.transform, false);
            var instance = Object.Instantiate(prefab, layer.transform, false);
            backdrops[instance] = backdrop;
            return instance;
        }

        public void Dismiss(GameObject instance)
        {
            if (backdrops.TryGetValue(instance, out var backdrop))
            {
                backdrops.Remove(instance);
                if (backdrop != null) Object.Destroy(backdrop);
            }
            if (instance != null) Object.Destroy(instance);
        }

        public void DismissAll()
        {
            backdrops.Clear();
            if (layer != null) Object.Destroy(layer);
            layer = null;
        }
    }
}
