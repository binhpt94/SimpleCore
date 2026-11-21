using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class AssetCatalogs : IAssetCatalogs
    {
        private readonly Dictionary<string, AssetCatalog> byKind = new Dictionary<string, AssetCatalog>();

        /// <summary>Checks every catalog; throws on a missing catalog, a catalog without a kind, or two catalogs of one kind.</summary>
        public AssetCatalogs(IEnumerable<AssetCatalog> catalogs)
        {
            foreach (var catalog in catalogs ?? Array.Empty<AssetCatalog>())
            {
                if (catalog == null)
                    throw new InvalidOperationException("An asset catalog slot on Boot is empty.");
                if (string.IsNullOrEmpty(catalog.Kind))
                    throw new InvalidOperationException($"Asset catalog '{catalog.name}' has no kind.");
                if (byKind.TryGetValue(catalog.Kind, out var other))
                    throw new InvalidOperationException(
                        $"Asset catalogs '{other.name}' and '{catalog.name}' are both of kind '{catalog.Kind}'. One per kind and scene.");
                catalog.Check();
                byKind.Add(catalog.Kind, catalog);
            }
        }

        public AssetCatalog Get(string kind)
        {
            if (kind != null && byKind.TryGetValue(kind, out var catalog)) return catalog;
            throw new InvalidOperationException(
                $"This scene has no asset catalog of kind '{kind}'. Add one to the scene's Boot (_Composition).");
        }
    }
}
