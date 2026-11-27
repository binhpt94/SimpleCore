namespace Game.Core
{
    /// <summary>The asset catalogs of the current scene, by kind. Boot registers it before any installer runs.</summary>
    public interface IAssetCatalogs
    {
        /// <summary>The scene's catalog of <paramref name="kind"/>; throws, naming the kind, when the scene has none.</summary>
        AssetCatalog Get(string kind);

        /// <summary>The scene's catalog of <paramref name="kind"/>, or false when the scene has none. For optional catalogs.</summary>
        bool TryGet(string kind, out AssetCatalog catalog);
    }
}
