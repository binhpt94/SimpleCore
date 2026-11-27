using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Services.Popup.Tests
{
    /// <summary>The installer as a cheat list source (Core's ICheatStateSource) and how it follows Core's ICheatSelection.</summary>
    public class PopupInstallerCheatTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void RealInstaller_ListsTheServiceAndItsStatesInTheOrderOfTheCheatNode()
        {
            ICheatStateSource source = PopupServiceTests.LoadInstallerAsset();

            Assert.That(source.CheatFeatureTitle, Is.EqualTo("Dịch vụ popup"));
            Assert.That(source.CheatStates.Select(s => s.Key), Is.EqualTo(new[] { "Popup/SamplePopup" }));
            Assert.That(source.CheatStates[0].Title, Does.StartWith("Popup mẫu của dịch vụ đang mở trên scene đang chạy"));
        }

        [Test]
        public void MissingCheatSelection_IsNamedAtTheStartOfInstall()
        {
            var error = Assert.Throws<InvalidOperationException>(
                () => PopupServiceTests.LoadInstallerAsset().Install(new ServiceRegistry()));

            Assert.That(error.Message, Does.Contain(nameof(ICheatSelection)));
        }

        [Test]
        public void PickOfItsOwnState_BuildsTheSamplePopup_AndReportsItRunning()
        {
            var cheats = new FakeCheatSelection();
            cheats.Pick("Popup/SamplePopup");
            var session = new PopupServiceTests.FakeSession();

            Installer(PopupCheatState.None).Install(Registry(session, cheats));

            Assert.That(session.Joined.OfType<SamplePopupCheat>().Count(), Is.EqualTo(1));
            Assert.That(cheats.Reported, Is.EqualTo(new[] { "Popup/SamplePopup" }));
        }

        [TestCase("PopupLose/NewRecord")]
        [TestCase(null)]
        public void PickOfAnotherFeatureOrNormalPlay_BuildsNothing_EvenWhenTheAssetChoseAState(string picked)
        {
            var cheats = new FakeCheatSelection();
            cheats.Pick(picked);
            var session = new PopupServiceTests.FakeSession();

            Installer(PopupCheatState.SamplePopup).Install(Registry(session, cheats));

            Assert.That(session.Joined.OfType<SamplePopupCheat>(), Is.Empty);
            Assert.That(session.Joined, Has.Count.EqualTo(1), "only the popup service's own part");
            Assert.That(cheats.Reported, Is.Empty);
        }

        [Test]
        public void NoPick_StateChosenOnTheAsset_IsBuiltAndReported()
        {
            var cheats = new FakeCheatSelection();
            var session = new PopupServiceTests.FakeSession();

            Installer(PopupCheatState.SamplePopup).Install(Registry(session, cheats));

            Assert.That(session.Joined.OfType<SamplePopupCheat>().Count(), Is.EqualTo(1));
            Assert.That(cheats.Reported, Is.EqualTo(new[] { "Popup/SamplePopup" }));
        }

        [Test]
        public void NoPick_NoStateOnTheAsset_BuildsAndReportsNothing()
        {
            var cheats = new FakeCheatSelection();
            var session = new PopupServiceTests.FakeSession();

            Installer(PopupCheatState.None).Install(Registry(session, cheats));

            Assert.That(session.Joined.OfType<SamplePopupCheat>(), Is.Empty);
            Assert.That(cheats.Reported, Is.Empty);
        }

        /// <summary>A copy of the real installer asset (real config) with the cheat field set.</summary>
        private PopupInstaller Installer(PopupCheatState state)
        {
            var installer = Object.Instantiate(PopupServiceTests.LoadInstallerAsset());
            created.Add(installer);
            var so = new SerializedObject(installer);
            so.FindProperty("cheat").enumValueIndex = (int)state;
            so.ApplyModifiedPropertiesWithoutUndo();
            return installer;
        }

        private ServiceRegistry Registry(ISessionScene session, ICheatSelection cheats)
        {
            var catalog = ScriptableObject.CreateInstance<AssetCatalog>();
            created.Add(catalog);
            var so = new SerializedObject(catalog);
            so.FindProperty("kind").stringValue = CatalogPopupPrefabs.Kind;
            so.ApplyModifiedPropertiesWithoutUndo();

            var registry = new ServiceRegistry();
            registry.Register<ICheatSelection>(cheats);
            registry.Register<IAssetCatalogs>(new AssetCatalogs(new[] { catalog }));
            registry.Register<ISessionScene>(session);
            return registry;
        }
    }

    /// <summary>Stand-in for Core's ICheatSelection: records every ReportRunning.</summary>
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
