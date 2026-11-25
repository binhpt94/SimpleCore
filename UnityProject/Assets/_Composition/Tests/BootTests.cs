using System.Linq;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Composition.Tests
{
    public class BootTests
    {
        private const string BasePath = "Assets/_Composition/Boot.prefab";
        private const string SessionScene = "SessionSceneInstaller";

        private static readonly object[] Scenes =
        {
            new object[] { "Assets/_Composition/Boot.Loading.prefab", "Assets/_Composition/Loading.unity", "SceneFlowLoadingInstaller", 2 },
            new object[] { "Assets/_Composition/Boot.Game.prefab", "Assets/_Composition/Game.unity", "SceneFlowGameInstaller", 14 },
        };

        [Test]
        public void BasePrefab_ListsNothing_EachSceneStatesItsOwnLists()
        {
            var boot = Load(BasePath);

            Assert.That(boot.Installers, Is.Empty);
            Assert.That(boot.Catalogs, Is.Empty);
        }

        [TestCaseSource(nameof(Scenes))]
        public void Variant_IsAVariantOfBoot_WithNoEmptySlots(string variant, string scene, string last, int count)
        {
            var boot = Load(variant);

            Assert.That(PrefabUtility.GetPrefabAssetType(boot.gameObject), Is.EqualTo(PrefabAssetType.Variant));
            Assert.That(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(boot.gameObject)),
                Is.EqualTo(BasePath));
            Assert.That(boot.Installers.Count, Is.EqualTo(count));
            Assert.That(boot.Installers, Has.None.Null);
            Assert.That(boot.Catalogs, Has.None.Null);
        }

        [TestCaseSource(nameof(Scenes))]
        public void Variant_InstallsSessionSceneFirstAndItsSceneFlowSideLast(string variant, string scene, string last, int count)
        {
            var names = Load(variant).Installers.Select(i => i.name).ToList();

            Assert.That(names.First(), Is.EqualTo(SessionScene));
            Assert.That(names.Last(), Is.EqualTo(last));
        }

        // feature-session-scene/session-steps/participant-order: the scene's participants join in install order,
        // between SessionScene (first) and the scene's SceneFlow side (last).
        [Test]
        public void LoadingScene_HasOnlySceneFlowAsParticipant()
        {
            var names = Load("Assets/_Composition/Boot.Loading.prefab").Installers.Select(i => i.name).ToList();

            Assert.That(names, Is.EqualTo(new[] { SessionScene, "SceneFlowLoadingInstaller" }));
        }

        [Test]
        public void GameScene_AtIdea_JoinsTwelveFeaturesInTheDesignedOrder()
        {
            var names = Load("Assets/_Composition/Boot.Game.prefab").Installers.Select(i => i.name).ToList();

            Assert.That(names, Is.EqualTo(new[]
            {
                SessionScene,
                "PopupInstaller", "RunInstaller", "WalletInstaller", "BattlefieldInstaller", "EnemyInstaller",
                "HeroInstaller", "CastleInstaller", "WaveInstaller", "UpgradeInstaller", "HudInstaller",
                "HudUpgradeInstaller", "PopupLoseInstaller",
                "SceneFlowGameInstaller",
            }));
        }

        [TestCaseSource(nameof(Scenes))]
        public void Variant_RunsEveryInstallerWithoutMissingDependencies(string variant, string scene, string last, int count)
        {
            // Popup reads the scene's popup catalog and Wallet resolves the session and the run during Install,
            // so a wrong order or a missing catalog throws here.
            Assert.DoesNotThrow(() => Load(variant).Run());
        }

        [TestCaseSource(nameof(Scenes))]
        public void Scene_UsesItsVariantWithoutOverridingTheLists(string variant, string scene, string last, int count)
        {
            var text = System.IO.File.ReadAllText(scene);
            var guid = AssetDatabase.AssetPathToGUID(variant);

            Assert.That(text, Does.Contain("guid: " + guid), $"{scene} should place {variant}");
            Assert.That(text, Does.Not.Contain("guid: " + AssetDatabase.AssetPathToGUID(BasePath)),
                $"{scene} should not place Boot.prefab itself");
            Assert.That(text, Does.Not.Contain("installers.Array").And.Not.Contain("catalogs.Array"),
                $"{scene} overrides Boot's lists; change {variant} instead");
        }

        [Test]
        public void GameScene_PopupCatalogHasTheLosePopup()
        {
            var catalogs = new AssetCatalogs(Load("Assets/_Composition/Boot.Game.prefab").Catalogs);

            Assert.That(catalogs.Get("Popup").TryGet("PopupLose/Main", out var prefab), Is.True);
            Assert.That(prefab, Is.InstanceOf<GameObject>());
        }

        private static Boot Load(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path + " missing");
            var boot = prefab.GetComponent<Boot>();
            Assert.That(boot, Is.Not.Null, "Boot component missing on " + path);
            return boot;
        }
    }
}
