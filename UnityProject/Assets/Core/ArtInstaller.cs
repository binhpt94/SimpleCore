using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Registers <see cref="IArt"/> from the scene's catalog of kind "Art". Listed on every Boot variant right after
    /// SessionScene, so every feature can resolve IArt in its Install. A scene without an Art catalog gets GreyBox only.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Art Installer")]
    public sealed class ArtInstaller : InstallerAsset
    {
        public override void Install(IServiceRegistry registry)
        {
            AssetCatalog catalog = null;
            if (registry.TryResolve<IAssetCatalogs>(out var catalogs)) catalogs.TryGet(Art.Kind, out catalog);
            registry.Register<IArt>(new Art(catalog));
        }
    }
}
