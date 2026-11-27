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
        private const string Art = "ArtInstaller";

        private static readonly object[] Scenes =
        {
            new object[] { "Assets/_Composition/Boot.Loading.prefab", "Assets/_Composition/Loading.unity", "SceneFlowLoadingInstaller", 4 },
            new object[] { "Assets/_Composition/Boot.Game.prefab", "Assets/_Composition/Game.unity", "SceneFlowGameInstaller", 23 },
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
        // between SessionScene (first) and the scene's SceneFlow side (last). Loading now also has CheatPanel before SceneFlow;
        // ArtInstaller right after SessionScene only registers IArt, it is not a participant;
        // the test keeps its name because the requirement book cites it.
        [Test]
        public void LoadingScene_HasOnlySceneFlowAsParticipant()
        {
            var names = Load("Assets/_Composition/Boot.Loading.prefab").Installers.Select(i => i.name).ToList();

            Assert.That(names, Is.EqualTo(new[] { SessionScene, Art, "CheatPanelInstaller", "SceneFlowLoadingInstaller" }));
        }

        // feature-session-scene/session-steps/participant-order#game-list: at IDEA the Game scene has 16 participants
        // in this order (plus SaveInstaller before Run: not a participant, it only registers ISavedRecord for the run): SimClock first (no view, drives the shared clock each frame, so every gameplay feature can
        // resolve it), then 13 features (PopupRating after PopupLose, it opens over it), then CheatPanel (its button sits in every scene),
        // then scene-flow last (a participant with no view). SessionScene installs first and runs the session; it is not a participant.
        // The design table still lists 15 (CheatPanel not yet in it): the design needs feature-cheat-panel added before scene-flow.
        [Test]
        public void GameScene_AtIdea_HasSixteenParticipantsInTheDesignedOrder_SimClockFirst_CheatPanelThenSceneFlowLast()
        {
            var names = Load("Assets/_Composition/Boot.Game.prefab").InstallersAt(GameStage.Idea).Select(i => i.name).ToList();

            Assert.That(names, Is.EqualTo(new[]
            {
                SessionScene, Art, SimClock,
                "PopupInstaller", "SaveInstaller", "RunInstaller", "WalletInstaller", "BattlefieldInstaller", "EnemyInstaller",
                "HeroInstaller", "CastleInstaller", "WaveInstaller", "UpgradeInstaller", "HudInstaller",
                "HudUpgradeInstaller", "PopupLoseInstaller", "PopupRatingInstaller",
                "CheatPanelInstaller", "SceneFlowGameInstaller",
            }));
            Assert.That(names.Skip(1).Count(n => n != "SaveInstaller" && n != Art), Is.EqualTo(16),
                "participants after SessionScene (Save only registers ISavedRecord and Art only registers IArt, they are not participants); CheatPanel holds the cheat overlay of every scene");
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
                SessionScene, Art, SimClock,
                "PopupInstaller", "SaveInstaller", "RunInstaller", "WalletInstaller", "BattlefieldInstaller", "CombatInstaller",
                "EnemyInstaller", "HeroInstaller", "CastleInstaller", "WaveInstaller", "UpgradeInstaller", "HudInstaller",
                "HudUpgradeInstaller", "PopupLoseInstaller", "PopupRatingInstaller", "HomeInstaller", "TeamSelectInstaller", "GameFlowInstaller", "CheatPanelInstaller", "SceneFlowGameInstaller",
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

        // feature-cheat-panel/cheat-states/states-rules: the cheat list holds the states of every scene, with the
        // scene to reload, and every scene shares one selection.
        [TestCaseSource(nameof(Scenes))]
        public void Variant_SharesTheCheatSelectionAndKnowsEveryScene(string variant, string scene, string last, int count)
        {
            var boot = Load(variant);
            var selection = new SerializedObject(boot).FindProperty("cheatSelection").objectReferenceValue;

            Assert.That(selection, Is.InstanceOf<CheatSelectionAsset>());
            Assert.That(boot.CheatScenes.Select(s => s.scene), Is.EqualTo(new[] { "Loading", "Game" }));
            Assert.That(boot.CheatScenes.Select(s => AssetDatabase.GetAssetPath(s.boot.gameObject)),
                Is.EqualTo(new[] { "Assets/_Composition/Boot.Loading.prefab", "Assets/_Composition/Boot.Game.prefab" }));
        }

        [TestCaseSource(nameof(Scenes))]
        public void CheatCatalog_ListsEveryCheatStateSourceOfTheScene_WithThatScene(string variant, string scene, string last, int count)
        {
            var boot = Load(variant);
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(scene);
            var catalog = Load(BasePath).CheatSourcesAt(GameStage.Idea);
            var sources = boot.InstallersAt(GameStage.Idea).OfType<ICheatStateSource>().ToList();

            foreach (var source in sources)
            {
                var entry = catalog.Where(c => c.Source == source).ToList();
                Assert.That(entry.Count, Is.EqualTo(1), ((Object)source).name + " is not in the cheat catalog once");
                Assert.That(entry[0].Scene, Is.EqualTo(sceneName), ((Object)source).name);
            }
        }

        [Test]
        public void CheatCatalog_LeavesOutInstallersSkippedAtTheStage()
        {
            var game = Load("Assets/_Composition/Boot.Game.prefab");
            var catalog = Load(BasePath).CheatSourcesAt(GameStage.Idea);

            foreach (var later in game.FromPrototype.OfType<ICheatStateSource>())
                Assert.That(catalog.Any(c => c.Source == later), Is.False, ((Object)later).name);
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
