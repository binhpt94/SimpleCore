using System.Linq;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Composition.Tests
{
    public class ArtWiringTests
    {
        private const string InstallerPath = "Assets/_Composition/ArtInstaller.asset";
        private const string CatalogPath = "Assets/_Composition/Game.Art.asset";

        private static readonly string[] Variants =
        {
            "Assets/_Composition/Boot.Loading.prefab",
            "Assets/_Composition/Boot.Game.prefab",
        };

        [Test]
        public void ArtInstaller_WithTheRealArtCatalog_RegistersArtThatShowsGreyBox()
        {
            var installer = AssetDatabase.LoadAssetAtPath<ArtInstaller>(InstallerPath);
            var catalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(CatalogPath);
            Assert.That(installer, Is.Not.Null, InstallerPath);
            Assert.That(catalog, Is.Not.Null, CatalogPath);
            var registry = new ServiceRegistry();
            registry.Register<IAssetCatalogs>(new AssetCatalogs(new[] { catalog }));

            installer.Install(registry);

            Assert.That(catalog.Kind, Is.EqualTo("Art"));
            Assert.That(registry.Resolve<IArt>().TierOf("Hero/Knight"), Is.EqualTo(ArtTier.GreyBox));
        }

        [TestCaseSource(nameof(Variants))]
        public void EveryScene_InstallsArtRightAfterSessionScene_WithTheArtCatalog(string variant)
        {
            var boot = AssetDatabase.LoadAssetAtPath<GameObject>(variant).GetComponent<Boot>();

            Assert.That(boot.Installers.Select(i => i.name).Take(2), Is.EqualTo(new[] { "SessionSceneInstaller", "ArtInstaller" }));
            Assert.That(boot.Catalogs.Select(AssetDatabase.GetAssetPath), Does.Contain(CatalogPath));
            Assert.That(boot.Run().Resolve<IArt>(), Is.Not.Null);
        }
    }
}
