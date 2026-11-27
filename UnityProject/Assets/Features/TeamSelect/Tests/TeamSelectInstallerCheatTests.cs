using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Hero;
using NUnit.Framework;
using UnityEditor;

namespace Game.TeamSelect.Tests
{
    /// <summary>The cheat list (ICheatStateSource) and the pick (ICheatSelection) on the real installer asset.</summary>
    public sealed class TeamSelectInstallerCheatTests
    {
        private const string InstallerPath = "Assets/Features/TeamSelect/FeatureAssets/Public/TeamSelectInstaller.asset";

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
            public Game.Core.SessionState State => Game.Core.SessionState.Init;
            public event Action<Game.Core.SessionState> StateChanged { add { } remove { } }
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public UniTask EndAsync() => UniTask.CompletedTask;
        }

        private static TeamSelectInstaller LoadInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<TeamSelectInstaller>(InstallerPath);
            Assert.IsNotNull(installer, "missing " + InstallerPath);
            return installer;
        }

        /// <summary>Installs with the asset's cheat set to <paramref name="onAsset"/> (then puts the asset back) and returns the joined session.</summary>
        private static TeamSelectSession Install(FakeCheatSelection cheats, TeamSelectCheatState onAsset)
        {
            var installer = LoadInstaller();
            var before = installer.Cheat;
            try
            {
                installer.Cheat = onAsset;
                var registry = new ServiceRegistry();
                registry.Register<ICheatSelection>(cheats);
                registry.Register<IHeroRoster>(new FakeRoster());
                var session = new FakeSession();
                registry.Register<ISessionScene>(session);

                installer.Install(registry);

                Assert.AreEqual(1, session.Joined.Count);
                return (TeamSelectSession)session.Joined[0];
            }
            finally
            {
                installer.Cheat = before;
            }
        }

        private static void AssertFullTeamBuilt(TeamSelectSession session)
        {
            Assert.IsTrue(session.Presenter.IsOpen, "the screen is open");
            CollectionAssert.AreEqual(Fakes.DefaultTeam, session.Memory.Team, "remembered team: Knight, Axeman, Archer");
            Assert.IsTrue(session.Presenter.CanStart, "Enter battle is lit");
        }

        private static void AssertNothingBuilt(TeamSelectSession session)
        {
            Assert.IsFalse(session.Presenter.IsOpen, "the screen waits for the flow");
            Assert.IsFalse(session.Memory.HasTeam, "no team remembered");
        }

        [Test]
        public void RealInstallerKeepsTheCheatOff()
        {
            Assert.AreEqual(TeamSelectCheatState.None, LoadInstaller().Cheat, "the shipped asset builds no cheat state");
        }

        [Test]
        public void MissingCheatSelectionIsNamedAtTheStartOfInstall()
        {
            var registry = new ServiceRegistry();
            var session = new FakeSession();
            registry.Register<IHeroRoster>(new FakeRoster());
            registry.Register<ISessionScene>(session);

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(registry));

            StringAssert.Contains(nameof(ICheatSelection), error.Message);
            Assert.AreEqual(0, session.Joined.Count, "nothing joined");
        }

        [Test]
        public void RealInstallerListsTheFeatureAndItsStatesInTheOrderOfTheCheatNode()
        {
            ICheatStateSource source = LoadInstaller();

            Assert.AreEqual("Chọn đội", source.CheatFeatureTitle);
            Assert.AreEqual(1, source.CheatStates.Count);
            Assert.AreEqual("TeamSelect/FullTeam", source.CheatStates[0].Key);
            Assert.AreEqual("Màn chọn đội đang mở, đội nhớ đã điền đủ 3 chỗ, nút \"Vào trận\" sáng", source.CheatStates[0].Title);
        }

        [Test]
        public void PickOfThisFeatureBuildsTheFullTeamStateAndReportsIt()
        {
            var cheats = new FakeCheatSelection();
            cheats.Pick("TeamSelect/FullTeam");

            AssertFullTeamBuilt(Install(cheats, TeamSelectCheatState.None));
            CollectionAssert.AreEqual(new[] { "TeamSelect/FullTeam" }, cheats.Reported);
        }

        [Test]
        public void StateChosenOnTheAssetIsBuiltAndReportedWhileNothingWasPicked()
        {
            var cheats = new FakeCheatSelection();

            AssertFullTeamBuilt(Install(cheats, TeamSelectCheatState.FullTeam));
            CollectionAssert.AreEqual(new[] { "TeamSelect/FullTeam" }, cheats.Reported);
        }

        [TestCase("Home/WithRecord")]
        [TestCase(null)]
        public void PickOfAnotherFeatureOrNormalPlayBuildsNothingEvenWithACheatOnTheAsset(string pickedKey)
        {
            var cheats = new FakeCheatSelection();
            cheats.Pick(pickedKey);

            AssertNothingBuilt(Install(cheats, TeamSelectCheatState.FullTeam));
            Assert.IsEmpty(cheats.Reported);
        }

        [Test]
        public void NothingPickedAndNothingOnTheAssetBuildsNothing()
        {
            var cheats = new FakeCheatSelection();

            AssertNothingBuilt(Install(cheats, TeamSelectCheatState.None));
            Assert.IsEmpty(cheats.Reported);
        }
    }

    /// <summary>Stand-in for Core's cheat selection: records what the installer reports.</summary>
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
