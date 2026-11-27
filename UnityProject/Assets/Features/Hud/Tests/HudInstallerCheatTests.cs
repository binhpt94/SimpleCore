using System;
using System.Collections.Generic;
using Game.Castle;
using Game.Core;
using Game.Run;
using NUnit.Framework;
using UnityEditor;

namespace Game.Hud.Tests
{
    /// <summary>The cheat list (ICheatStateSource) and the pick (ICheatSelection) on the real installer asset.</summary>
    public sealed class HudInstallerCheatTests
    {
        private const string InstallerPath = "Assets/Features/Hud/FeatureAssets/Public/HudInstaller.asset";

        private static HudInstaller LoadInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<HudInstaller>(InstallerPath);
            Assert.IsNotNull(installer, "missing " + InstallerPath);
            return installer;
        }

        private static FakeRegistry FullRegistry(FakeCheatSelection cheats, out FakeSessionScene scene) =>
            FullRegistry(cheats, new FakeCheatStart(), out scene);

        private static FakeRegistry FullRegistry(FakeCheatSelection cheats, FakeCheatStart start, out FakeSessionScene scene)
        {
            scene = new FakeSessionScene();
            var castle = new FakeCastle();
            var skills = new FakeSkills();
            var wallet = new FakeWallet();
            var waves = new FakeWaves();
            var registry = new FakeRegistry();
            registry.Register<ICheatSelection>(cheats);
            registry.Register<ISessionScene>(scene);
            registry.Register<IRunFlow>(new FakeRun());
            registry.Register<ICastle>(castle);
            registry.Register<ICastleSkills>(skills);
            registry.Register<Game.Wallet.IWalletBalance>(wallet);
            registry.Register<Game.Wallet.IWalletIncome>(wallet);
            registry.Register<Game.Wallet.IWalletService>(wallet);
            registry.Register<Game.Wave.IWaveProgress>(waves);
            registry.Register<ICastleCheat>(new FakeCastleCheat(castle, skills));
            registry.Register<Game.Wave.IWaveCheat>(waves);
            registry.Register<IRunCheatStart>(start);
            return registry;
        }

        /// <summary>Installs with the asset's cheat set to <paramref name="onAsset"/>, then puts the asset back.</summary>
        private static HudSession Install(FakeCheatSelection cheats, HudCheatState onAsset) =>
            Install(cheats, onAsset, new FakeCheatStart());

        private static HudSession Install(FakeCheatSelection cheats, HudCheatState onAsset, FakeCheatStart start)
        {
            var installer = LoadInstaller();
            var before = installer.Cheat;
            try
            {
                installer.Cheat = onAsset;
                installer.Install(FullRegistry(cheats, start, out var scene));
                Assert.AreEqual(1, scene.Joined.Count);
                Assert.IsInstanceOf<HudSession>(scene.Joined[0]);
                return (HudSession)scene.Joined[0];
            }
            finally
            {
                installer.Cheat = before;
            }
        }

        [Test]
        public void RealInstallerKeepsTheCheatOff()
        {
            Assert.AreEqual(HudCheatState.None, LoadInstaller().Cheat, "the shipped asset builds no cheat state");
        }

        [Test]
        public void MissingCheatSelectionIsNamedAtTheStartOfInstall()
        {
            var registry = new FakeRegistry();

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(registry));

            StringAssert.Contains(nameof(ICheatSelection), error.Message);
        }

        [Test]
        public void RealInstallerListsTheFeatureAndItsStatesInTheOrderOfTheCheatNode()
        {
            ICheatStateSource source = LoadInstaller();

            Assert.AreEqual("HUD trận", source.CheatFeatureTitle);
            Assert.AreEqual(1, source.CheatStates.Count);
            Assert.AreEqual("Hud/MidBattle", source.CheatStates[0].Key);
            Assert.AreEqual("Đợt 7 vừa bắt đầu, thành còn 30% máu, Băng giá đang hồi", source.CheatStates[0].Title);
        }

        [Test]
        public void PickOfThisFeatureBuildsThatStateAndReportsIt()
        {
            var cheats = new FakeCheatSelection();
            cheats.Pick("Hud/MidBattle");

            var session = Install(cheats, HudCheatState.None);

            Assert.IsTrue(session.CheatPending, "mid-battle is built");
            CollectionAssert.AreEqual(new[] { "Hud/MidBattle" }, cheats.Reported);
        }

        [Test]
        public void StateChosenOnTheAssetIsBuiltAndReportedWhileNothingWasPicked()
        {
            var cheats = new FakeCheatSelection();

            var session = Install(cheats, HudCheatState.MidBattle);

            Assert.IsTrue(session.CheatPending);
            CollectionAssert.AreEqual(new[] { "Hud/MidBattle" }, cheats.Reported);
        }

        [TestCase("PopupLose/NewRecord")]
        [TestCase(null)]
        public void PickOfAnotherFeatureOrNormalPlayBuildsNothingEvenWithACheatOnTheAsset(string pickedKey)
        {
            var cheats = new FakeCheatSelection();
            cheats.Pick(pickedKey);

            var session = Install(cheats, HudCheatState.MidBattle);

            Assert.IsFalse(session.CheatPending, "no state was built");
            Assert.IsEmpty(cheats.Reported);
        }

        // Bug b20261127-ab7a: picking "Giữa trận" opened the start screen; the state only showed after Play then
        // Enter battle. Building the state now asks the run to open the scene straight in battle.
        [Test]
        public void PickedMidBattleAsksToOpenStraightInBattleWithTheDefaultTeam()
        {
            var cheats = new FakeCheatSelection();
            cheats.Pick("Hud/MidBattle");
            var start = new FakeCheatStart();

            Install(cheats, HudCheatState.None, start);

            Assert.AreEqual(1, start.Requests.Count, "asked for a battle once");
            Assert.AreEqual(0, start.Requests[0].Count, "empty team: the run's default team");
        }

        [TestCase("PopupLose/NewRecord")]
        [TestCase(null)]
        public void NoHudStateBuiltAsksForNoBattle(string pickedKey)
        {
            var cheats = new FakeCheatSelection();
            cheats.Pick(pickedKey);
            var start = new FakeCheatStart();

            Install(cheats, HudCheatState.MidBattle, start);

            Assert.IsEmpty(start.Requests);
        }

        [Test]
        public void NothingPickedAndNothingOnTheAssetBuildsNothing()
        {
            var cheats = new FakeCheatSelection();

            var session = Install(cheats, HudCheatState.None);

            Assert.IsFalse(session.CheatPending);
            Assert.IsEmpty(cheats.Reported);
        }

        /// <summary>Stand-in for the run's IRunCheatStart; records every request.</summary>
        internal sealed class FakeCheatStart : IRunCheatStart
        {
            public readonly List<RunTeam> Requests = new List<RunTeam>();
            public bool BattleRequested => Requests.Count > 0;
            public RunTeam Team => Requests.Count > 0 ? Requests[Requests.Count - 1] : default;

            public void RequestBattle(RunTeam team) => Requests.Add(team);
        }

        /// <summary>Stand-in for Core's cheat selection; records every ReportRunning.</summary>
        internal sealed class FakeCheatSelection : ICheatSelection
        {
            public readonly List<string> Reported = new List<string>();
            public bool HasPick { get; private set; }
            public string PickedKey { get; private set; }
            public string RunningKey { get; private set; }

            public void Pick(string key)
            {
                PickedKey = string.IsNullOrEmpty(key) ? null : key;
                HasPick = true;
            }

            public void ReportRunning(string key)
            {
                Reported.Add(key);
                RunningKey = key;
            }

            public void BeginScene() => RunningKey = null;
        }
    }
}
