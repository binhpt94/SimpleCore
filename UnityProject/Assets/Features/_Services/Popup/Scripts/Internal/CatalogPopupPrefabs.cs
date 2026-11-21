using System;
using Game.Core;
using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>Reads popup prefabs from the scene's asset catalog of kind "Popup".</summary>
    internal sealed class CatalogPopupPrefabs : IPopupPrefabs
    {
        internal const string Kind = "Popup";

        private readonly AssetCatalog catalog;

        /// <summary>Throws, naming the catalog and the id, when an id is not a popup id or its asset is not a prefab.</summary>
        internal CatalogPopupPrefabs(AssetCatalog catalog)
        {
            this.catalog = catalog;
            foreach (var id in catalog.Ids)
            {
                if (!PopupId.IsValid(id))
                    throw new InvalidOperationException(
                        $"Popup catalog '{catalog.name}': '{id}' is not a popup id (<Owner>/<Name>).");
                if (!catalog.TryGet(id, out var asset) || !(asset is GameObject))
                    throw new InvalidOperationException($"Popup catalog '{catalog.name}': '{id}' is not a prefab.");
            }
        }

        public GameObject Get(PopupId id)
        {
            if (id.Value == null)
                throw new ArgumentException("No popup id given (default PopupId).", nameof(id));
            if (catalog.TryGet(id.Value, out var asset)) return (GameObject)asset;
            throw new InvalidOperationException(
                $"Popup '{id}' is not in popup catalog '{catalog.name}' of this scene. Add it in _Composition.");
        }
    }
}
