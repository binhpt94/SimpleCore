using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Composition
{
    /// <summary>
    /// Starts a scene: registers the scene's asset catalogs, then runs each feature's installer in list order.
    /// Each scene uses its own variant of Boot.prefab (Boot.Loading, Boot.Game); the variant's lists are
    /// that scene's boot order and shared assets. No game logic here.
    /// </summary>
    public sealed class Boot : MonoBehaviour
    {
        [SerializeField] private InstallerAsset[] installers = new InstallerAsset[0];
        [SerializeField] private AssetCatalog[] catalogs = new AssetCatalog[0];

        internal IReadOnlyList<InstallerAsset> Installers => installers;

        internal IReadOnlyList<AssetCatalog> Catalogs => catalogs;

        private void Awake()
        {
            Run();
        }

        internal IServiceRegistry Run()
        {
            var registry = new ServiceRegistry();
            registry.Register<IAssetCatalogs>(new AssetCatalogs(catalogs));
            foreach (var installer in installers)
            {
                installer.Install(registry);
            }
            return registry;
        }
    }
}
