using System;
using System.Collections.Generic;
using Game.Core;
using Game.Run;
using NUnit.Framework;
using UnityEditor;

namespace Game.Home.Tests
{
    /// <summary>The cheat list (ICheatStateSource) and the pick (ICheatSelection) on the real installer asset.</summary>
    public sealed class HomeInstallerCheatTests
    {
        private const string InstallerPath = "Assets/Features/Home/FeatureAssets/Public/HomeInstaller.asset";
        private const int SavedBest = 3;

        private static HomeInstaller LoadInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<HomeInstaller>(InstallerPath);
            Assert.IsNotNull(installer, "missing " + InstallerPath);
            return installer;
        }

        /// <summary>
        /// Installs with the asset's cheat set to <paramref name="onAsset"/> (then puts the asset back), shows the
        /// start screen on a stand-in view and returns the record line it shows. The device's saved best is 3.
        /// </summary>
        private static string InstallAndReadRecord(FakeCheatSelection cheats, HomeCheatState onAsset)
        {
            var installer = LoadInstaller();
            var before = installer.Cheat;
            try
            {
                installer.Cheat = onAsset;
                var registry = new ServiceRegistry();
                registry.Register<ICheatSelection>(cheats);
                registry.Register<IRunScore>(new FakeRunScore { Best = SavedBest });
                registry.Register<ISessionScene>(new FakeSessionScene());

                installer.Install(registry);

                Assert.IsTrue(registry.TryResolve<IHomeScreen>(out var screen));
                var presenter = (HomeScreenPresenter)screen;
                var display = new FakeHomeDisplay();
                presenter.Attach(display);
                presenter.Show();
                return display.Content.Record;
            }
            finally
            {
                installer.Cheat = before;
            }
        }

        [Test]
        public void RealInstallerKeepsTheCheatOff()
        {
            Assert.AreEqual(HomeCheatState.None, LoadInstaller().Cheat, "the shipped asset builds no cheat state");
        }

        [Test]
        public void MissingCheatSelectionIsNamedAtTheStartOfInstall()
        {
            var registry = new ServiceRegistry();

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(registry));

            StringAssert.Contains(nameof(ICheatSelection), error.Message);
        }

        [Test]
        public void RealInstallerListsTheFeatureAndItsStatesInTheOrderOfTheCheatNode()
        {
            ICheatStateSource source = LoadInstaller();

            Assert.AreEqual("Màn bắt đầu", source.CheatFeatureTitle);
            Assert.AreEqual(1, source.CheatStates.Count);
            Assert.AreEqual("Home/WithRecord", source.CheatStates[0].Key);
            Assert.AreEqual("Màn bắt đầu đang hiện, đã có kỉ lục", source.CheatStates[0].Title);
        }

        [Test]
        public void PickOfThisFeatureBuildsThatStateAndReportsIt()
        {
            var cheats = new FakeCheatSelection();
            cheats.Pick("Home/WithRecord");

            var record = InstallAndReadRecord(cheats, HomeCheatState.None);

            Assert.AreEqual("Kỉ lục: 15 đợt", record);
            CollectionAssert.AreEqual(new[] { "Home/WithRecord" }, cheats.Reported);
        }

        [Test]
        public void StateChosenOnTheAssetIsBuiltAndReportedWhileNothingWasPicked()
        {
            var cheats = new FakeCheatSelection();

            var record = InstallAndReadRecord(cheats, HomeCheatState.WithRecord);

            Assert.AreEqual("Kỉ lục: 15 đợt", record);
            CollectionAssert.AreEqual(new[] { "Home/WithRecord" }, cheats.Reported);
        }

        [TestCase("Hud/MidBattle")]
        [TestCase(null)]
        public void PickOfAnotherFeatureOrNormalPlayBuildsNothingEvenWithACheatOnTheAsset(string pickedKey)
        {
            var cheats = new FakeCheatSelection();
            cheats.Pick(pickedKey);

            var record = InstallAndReadRecord(cheats, HomeCheatState.WithRecord);

            Assert.AreEqual("Kỉ lục: 3 đợt", record, "the saved best shows: no state was built");
            Assert.IsEmpty(cheats.Reported);
        }

        [Test]
        public void NothingPickedAndNothingOnTheAssetBuildsNothing()
        {
            var cheats = new FakeCheatSelection();

            var record = InstallAndReadRecord(cheats, HomeCheatState.None);

            Assert.AreEqual("Kỉ lục: 3 đợt", record);
            Assert.IsEmpty(cheats.Reported);
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
