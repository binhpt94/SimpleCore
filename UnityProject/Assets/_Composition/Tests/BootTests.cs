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
        private const string SimClock = "SimClockInstaller";

        private static readonly object[] Scenes =
        {
            new object[] { "Assets/_Composition/Boot.Loading.prefab", "Assets/_Composition/Loading.unity", "SceneFlowLoadingInstaller", 2 },
            new object[] { "Assets/_Composition/Boot.Game.prefab", "Assets/_Composition/Game.unity", "SceneFlowGameInstaller", 20 },
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

        // feature-session-scene/session-steps/participant-order#game-list: at IDEA the Game scene has 15 participants
        // in this order: SimClock first (no view, drives the shared clock each frame, so every gameplay feature can
        // resolve it), then 13 features (PopupRating after PopupLose, it opens over it), then scene-flow last (a participant with no view). SessionScene installs
        // first and runs the session; it is not a participant.
        [Test]
        public void GameScene_AtIdea_HasFifteenParticipantsInTheDesignedOrder_SimClockFirst_SceneFlowLast()
        {
            var names = Load("Assets/_Composition/Boot.Game.prefab").InstallersAt(GameStage.Idea).Select(i => i.name).ToList();

            Assert.That(names, Is.EqualTo(new[]
            {
                SessionScene, SimClock,
                "PopupInstaller", "RunInstaller", "WalletInstaller", "BattlefieldInstaller", "EnemyInstaller",
                "HeroInstaller", "CastleInstaller", "WaveInstaller", "UpgradeInstaller", "HudInstaller",
                "HudUpgradeInstaller", "PopupLoseInstaller", "PopupRatingInstaller",
                "SceneFlowGameInstaller",
            }));
            Assert.That(names.Skip(1).Count(), Is.EqualTo(15), "participants after SessionScene");
        }

        // The build stays at PROTOTYPE (human, 2026-11-27): at IDEA Enemy needs Combat, which the design puts at
        // PROTOTYPE, so the Game scene could not boot. Kept until the stages of Combat and the PROTOTYPE features are reviewed.
        [Test]
        public void EveryScene_IsAtPrototype_SetOnceOnTheBasePrefab()
        {
            Assert.That(Load(BasePath).Stage, Is.EqualTo(GameStage.Prototype));
            foreach (var scene in Scenes)
            {
                var variant = (string)((object[])scene)[0];
                Assert.That(Load(variant).Stage, Is.EqualTo(GameStage.Prototype), variant);
            }
        }

        [TestCaseSource(nameof(Scenes))]
        public void Variant_FromPrototypeListsOnlyInstallersOfItsOwnList(string variant, string scene, string last, int count)
        {
            var boot = Load(variant);

            Assert.That(boot.FromPrototype, Has.None.Null);
            Assert.That(boot.FromPrototype, Is.SubsetOf(boot.Installers));
            Assert.That(boot.FromPrototype.Select(i => i.name), Has.None.EqualTo(SessionScene).And.None.EqualTo(last));
        }

        [Test]
        public void GameScene_FromPrototype_HoldsTheFeaturesThatStartAtPrototype()
        {
            var names = Load("Assets/_Composition/Boot.Game.prefab").FromPrototype.Select(i => i.name).ToList();

            Assert.That(names, Is.EquivalentTo(new[]
            {
                "HomeInstaller", "TeamSelectInstaller", "GameFlowInstaller", "CombatInstaller",
            }));
        }

        [TestCaseSource(nameof(Scenes))]
        public void Variant_AtPrototype_RunsEveryInstallerWithoutMissingDependencies(string variant, string scene, string last, int count)
        {
            Assert.DoesNotThrow(() => Load(variant).Run(GameStage.Prototype));
        }

        [Test]
        public void GameScene_AtPrototype_JoinsItsFeaturesInTheDesignedOrder()
        {
            var names = Load("Assets/_Composition/Boot.Game.prefab").Installers.Select(i => i.name).ToList();

            Assert.That(names, Is.EqualTo(new[]
            {
                SessionScene, SimClock,
                "PopupInstaller", "RunInstaller", "WalletInstaller", "BattlefieldInstaller", "CombatInstaller",
                "EnemyInstaller", "HeroInstaller", "CastleInstaller", "WaveInstaller", "UpgradeInstaller", "HudInstaller",
                "HudUpgradeInstaller", "PopupLoseInstaller", "PopupRatingInstaller", "HomeInstaller", "TeamSelectInstaller", "GameFlowInstaller", "SceneFlowGameInstaller",
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

        [Test]
        public void GameScene_PopupCatalogHasTheRatingPopup()
        {
            var catalogs = new AssetCatalogs(Load("Assets/_Composition/Boot.Game.prefab").Catalogs);

            Assert.That(catalogs.Get("Popup").TryGet("PopupRating/Main", out var prefab), Is.True);
            Assert.That(prefab, Is.InstanceOf<GameObject>());
        }

        // feature-popup/popup-request/request-rules#params, #id-list: the Game scene's popup ids by stage.
        // Every stage has the lose popup and the rating invite, which opens over it (2026-11-27).
        [Test]
        public void GameScene_AtIdea_PopupCatalogHasTheLoseAndRatingPopups()
        {
            var catalogs = new AssetCatalogs(Load("Assets/_Composition/Boot.Game.prefab").CatalogsAt(GameStage.Idea));

            Assert.That(catalogs.Get("Popup").Ids, Is.EquivalentTo(new[] { "PopupLose/Main", "PopupRating/Main" }));
        }

        [Test]
        public void GameScene_AtPrototype_PopupCatalogHasTheLoseAndRatingPopups()
        {
            var catalogs = new AssetCatalogs(Load("Assets/_Composition/Boot.Game.prefab").CatalogsAt(GameStage.Prototype));

            Assert.That(catalogs.Get("Popup").Ids, Is.EquivalentTo(new[] { "PopupLose/Main", "PopupRating/Main" }));
        }

        [TestCaseSource(nameof(Scenes))]
        public void Variant_PrototypeCatalogsHaveNoEmptySlot_AndRegisterOnePerKind(string variant, string scene, string last, int count)
        {
            var boot = Load(variant);

            Assert.That(boot.PrototypeCatalogs, Has.None.Null);
            Assert.DoesNotThrow(() => new AssetCatalogs(boot.CatalogsAt(GameStage.Prototype)));
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
