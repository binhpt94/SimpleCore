using System;
using Game.Core;
using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>
    /// Reads popup prefabs from the scene's asset catalog of kind "Popup". The catalog points straight at the
    /// prefabs, so they are loaded with the scene; preparing means checking every entry before play.
    /// </summary>
    internal sealed class CatalogPopupPrefabs : IPopupPrefabs
    {
        internal const string Kind = "Popup";

        private readonly AssetCatalog catalog;

        internal CatalogPopupPrefabs(AssetCatalog catalog)
        {
            this.catalog = catalog ? catalog : throw new ArgumentNullException(nameof(catalog));
        }

        public void Prepare()
        {
            catalog.Check();   // empty id, id listed twice, entry without asset: message names the catalog and id
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
            if (catalog.TryGet(id.Value, out var asset) && asset is GameObject prefab) return prefab;
            throw new InvalidOperationException(
                $"Popup '{id}' is not in popup catalog '{catalog.name}' of this scene. Add it in _Composition.");
        }
    }
}
