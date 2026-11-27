using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using SessionState = Game.Core.SessionState;

namespace Game.CheatPanel.Tests
{
    /// <summary>Node feature-cheat-panel/cheat-access/access-spec: config cheat_enabled, AccessParams, installer.</summary>
    public sealed class CheatAccessTests
    {
        private const string InstallerPath = "Assets/Features/CheatPanel/FeatureAssets/Public/CheatPanelInstaller.asset";
        private const string ConfigPath = "Assets/Features/CheatPanel/FeatureAssets/Private/CheatPanelConfig.asset";

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
                if (o != null) Object.DestroyImmediate(o);
            _created.Clear();
        }

        private T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _created.Add(o);
            return o;
        }

        private static CheatPanelConfig RealConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CheatPanelConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, $"config asset missing at {ConfigPath}");
            return config;
        }

        private static CheatPanelInstaller RealInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<CheatPanelInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"installer asset missing at {InstallerPath}");
            return installer;
        }

        private static ServiceRegistry RegistryWith(FakeSession session)
        {
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(session);
            return registry;
        }

        // spec-config-asset: the switch is a field of the feature's config asset, which the installer carries into the build.
        [Test]
        public void CheatEnabledSwitchLivesInTheConfigAssetTheInstallerShips()
        {
            Assert.That(RealInstaller().Config, Is.SameAs(RealConfig()), "installer points at FeatureAssets/Private/CheatPanelConfig");
            var field = new SerializedObject(RealConfig()).FindProperty("_cheatEnabled");
            Assert.That(field, Is.Not.Null, "cheat_enabled is a serialized field of the asset");
            Assert.That(field.propertyType, Is.EqualTo(SerializedPropertyType.Boolean), "cheat_enabled is an on/off box");
        }

        // spec-default-on: a new config and the shipped config both start with cheat_enabled on.
        [Test]
        public void CheatEnabledIsOnByDefault()
        {
            Assert.That(Make<CheatPanelConfig>().CheatEnabled, Is.True, "cheat_enabled default of a new config");
            Assert.That(RealConfig().CheatEnabled, Is.True, "cheat_enabled of the shipped config");
        }

        // AccessParams rows, IDEA column.
        [Test]
        public void AccessParams_CheatEnabled_IsTrue()
        {
            Assert.That(RealConfig().CheatEnabled, Is.True, "cheat_enabled");
        }

        [Test]
        public void AccessParams_ButtonLabel_IsCheat()
        {
            Assert.That(RealConfig().ButtonLabel, Is.EqualTo("Cheat"), "button_label");
        }

        [Test]
        public void AccessParams_PanelTitle_IsCheat()
        {
            Assert.That(RealConfig().PanelTitle, Is.EqualTo("Cheat"), "panel_title");
        }

        [Test]
        public void AccessParams_NormalRowLabel_IsChoiBinhThuong()
        {
            Assert.That(RealConfig().NormalRowLabel, Is.EqualTo("Chơi bình thường"), "normal_row_label");
        }

        [Test]
        public void AccessParams_EmptyLabel_SaysTheBuildHasNoCheatState()
        {
            Assert.That(RealConfig().EmptyLabel, Is.EqualTo("Bản này không có state cheat"), "empty_label");
        }

        // spec-any-build: with cheat_enabled on, the overlay is created; no build-type condition anywhere on that path.
        [Test]
        public void CheatEnabledOnCreatesTheOverlayWhenTheSceneRuns()
        {
            var installer = Make<CheatPanelInstaller>();
            installer.Config = Make<CheatPanelConfig>();
            var spawner = new FakeSpawner();
            installer.SpawnerOverride = spawner;
            var session = new FakeSession();

            installer.Install(RegistryWith(session));
            Assert.That(session.Joined, Has.Count.EqualTo(1), "joins the scene session");
            Assert.That(spawner.Spawned, Is.Zero, "nothing is created during Install");

            session.Joined[0].Run();
            Assert.That(spawner.Spawned, Is.EqualTo(1), "one overlay when the session runs");
            Assert.That(spawner.LastConfig, Is.SameAs(installer.Config), "the overlay gets the config texts");
        }

        [Test]
        public void OnlyCheatEnabledDecides_NoBuildTypeConditionInTheAccessCode()
        {
            foreach (var file in new[] { "CheatPanelInstaller.cs", "CheatOverlayParticipant.cs", "CheatPanelConfig.cs", "PrefabCheatOverlaySpawner.cs" })
            {
                var text = File.ReadAllText(Path.Combine("Assets/Features/CheatPanel/Scripts/Internal", file));
                Assert.That(text, Does.Not.Contain("UNITY_EDITOR"), file);
                Assert.That(text, Does.Not.Contain("DEVELOPMENT_BUILD"), file);
                Assert.That(text, Does.Not.Contain("Debug.isDebugBuild"), file);
                Assert.That(text, Does.Not.Contain("Application.isEditor"), file);
            }
        }

        [Test]
        public void TheOverlayIsDestroyedWhenTheSceneSessionEnds()
        {
            var installer = Make<CheatPanelInstaller>();
            installer.Config = Make<CheatPanelConfig>();
            var spawner = new FakeSpawner();
            installer.SpawnerOverride = spawner;
            var session = new FakeSession();
            installer.Install(RegistryWith(session));
            session.Joined[0].Run();

            session.Joined[0].DisposeAsync().GetAwaiter().GetResult();
            Assert.That(spawner.Despawned, Is.EqualTo(1));
            Assert.That(((ISessionParticipantUsage)session.Joined[0]).HeldViews, Is.Zero);
        }

        // spec-off-nothing and edge-config-tat: off = no button, no panel, no touch blocked, game as if the feature were absent.
        [Test]
        public void CheatEnabledOffCreatesNothingAndJoinsNothing()
        {
            var installer = Make<CheatPanelInstaller>();
            installer.Config = Make<CheatPanelConfig>();
            installer.Config.CheatEnabled = false;
            var spawner = new FakeSpawner();
            installer.SpawnerOverride = spawner;
            var session = new FakeSession();

            installer.Install(RegistryWith(session));

            Assert.That(session.Joined, Is.Empty, "no participant, so no button, panel or shade later");
            Assert.That(spawner.Spawned, Is.Zero, "no overlay");
        }

        [Test]
        public void ConfigOffNeedsNothingElse_NoSessionNoPrefab()
        {
            var installer = Make<CheatPanelInstaller>();
            installer.Config = Make<CheatPanelConfig>();
            installer.Config.CheatEnabled = false;
            Assert.DoesNotThrow(() => installer.Install(new ServiceRegistry()), "off resolves nothing and needs no overlay prefab");
        }

        // edge-thieu-asset-config: missing config fails at boot and names the config.
        [Test]
        public void MissingConfigFailsAtInstallNamingCheatPanelConfig()
        {
            var installer = Make<CheatPanelInstaller>();
            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(RegistryWith(new FakeSession())));
            Assert.That(error.Message, Does.Contain(nameof(CheatPanelConfig)));
        }

        [Test]
        public void CheatEnabledOnWithoutOverlayPrefabFailsAtInstallNamingThePrefab()
        {
            var installer = Make<CheatPanelInstaller>();
            installer.Config = Make<CheatPanelConfig>();
            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(RegistryWith(new FakeSession())));
            Assert.That(error.Message, Does.Contain("overlayPrefab"));
        }

        [Test]
        public void CheatEnabledOnWithoutSessionSceneFailsNamingIt()
        {
            var installer = Make<CheatPanelInstaller>();
            installer.Config = Make<CheatPanelConfig>();
            installer.SpawnerOverride = new FakeSpawner();
            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(new ServiceRegistry()));
            Assert.That(error.Message, Does.Contain(nameof(ISessionScene)));
        }

        [Test]
        public void RealInstallerAssetInstallsWithTheShippedConfig()
        {
            var installer = RealInstaller();
            var spawner = new FakeSpawner();
            installer.SpawnerOverride = spawner;
            try
            {
                var session = new FakeSession();
                installer.Install(RegistryWith(session));
                Assert.That(session.Joined, Has.Count.EqualTo(1), "shipped config has cheat_enabled on");
                session.Joined[0].Run();
                Assert.That(spawner.Spawned, Is.EqualTo(1));
            }
            finally { installer.SpawnerOverride = null; }
        }

        private sealed class FakeSpawner : ICheatOverlaySpawner
        {
            public int Spawned;
            public int Despawned;
            public CheatPanelConfig LastConfig;

            public GameObject Spawn(CheatPanelConfig config)
            {
                Spawned++;
                LastConfig = config;
                return new GameObject("FakeCheatOverlay");
            }

            public void Despawn(GameObject overlay)
            {
                Despawned++;
                if (overlay != null) Object.DestroyImmediate(overlay);
            }
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
            public SessionState State => SessionState.Init;
            public event Action<SessionState> StateChanged { add { } remove { } }
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public UniTask EndAsync() => UniTask.CompletedTask;
        }
    }
}
