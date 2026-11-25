using System;
using System.Collections.Generic;
using System.Threading;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Services.Popup.Tests
{
    /// <summary>feature-popup/cheat, state sample-popup.</summary>
    public class PopupCheatTests
    {
        private readonly List<Object> created = new List<Object>();
        private PopupService service;
        private Log log;
        private GameObject sample;

        [SetUp]
        public void SetUp()
        {
            sample = new GameObject("PopupDevSample");
            created.Add(sample);
            sample.AddComponent<FakeSampleView>();
            log = new Log();
            var prefabs = new DevSamplePrefabs(new ScenePrefabs(), sample);
            service = new PopupService(new Presenter(created), prefabs, log, 3);
            service.Start();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void SamplePopup_OpensTheServicesOwnSampleOnTopWhenTheSceneRuns()
        {
            var cheat = new SamplePopupCheat(service);

            cheat.Run();

            Assert.That(service.OpenCount, Is.EqualTo(1));
            Assert.That(service.IsOnTop(service.Top), Is.True);
            Assert.That(service.TryGetView<IDevSampleView>(service.Top, out _), Is.True);
            Assert.That(log.Errors, Is.Empty);
        }

        [Test]
        public void SamplePopup_CloseButton_ClosesItLikeAnyCaller_AndASecondClickIsIgnored()
        {
            new SamplePopupCheat(service).Run();
            service.TryGetView<FakeSampleView>(service.Top, out var view);

            view.Click();
            view.Click();

            Assert.That(service.OpenCount, Is.EqualTo(0));
            Assert.That(log.Errors, Is.Empty);
        }

        [Test]
        public void SamplePopup_SessionEnds_TheCloseButtonNoLongerReachesTheService()
        {
            var cheat = new SamplePopupCheat(service);
            cheat.Run();
            service.TryGetView<FakeSampleView>(service.Top, out var view);

            cheat.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(view.Listeners, Is.EqualTo(0));
        }

        [Test]
        public void DevSamplePrefabs_KeepsTheScenesPopups()
        {
            var scenePopup = new GameObject("A");
            created.Add(scenePopup);
            var scene = new ScenePrefabs { [new PopupId("Test/A")] = scenePopup };
            var prefabs = new DevSamplePrefabs(scene, sample);

            Assert.That(prefabs.TryGet(new PopupId("Test/A"), out var found) && found == scenePopup, Is.True);
            Assert.That(prefabs.TryGet(PopupCheat.DevSample, out var dev) && dev == sample, Is.True);
            Assert.That(prefabs.TryGet(new PopupId("Test/B"), out _), Is.False);
        }

        [Test]
        public void DevSamplePrefabs_SceneCatalogAlsoListingTheSampleId_FailsTheLoadNamingIt()
        {
            var scene = new ScenePrefabs { [PopupCheat.DevSample] = sample };

            var error = Assert.Throws<InvalidOperationException>(new DevSamplePrefabs(scene, sample).Prepare);

            Assert.That(error.Message, Does.Contain("_Services/Popup/DevSample"));
        }

        [Test]
        public void Installer_SamplePopupWithoutSamplePrefab_FailsNamingTheField()
        {
            var config = ScriptableObject.CreateInstance<PopupConfig>();
            created.Add(config);

            var error = Assert.Throws<InvalidOperationException>(() => PopupCheat.Check(PopupCheatState.SamplePopup, config));

            Assert.That(error.Message, Does.Contain("devSamplePrefab"));
            Assert.DoesNotThrow(() => PopupCheat.Check(PopupCheatState.None, config));
        }

        [Test]
        public void InstallerAsset_HasTheSamplePopupStateOff_AndItsConfigHoldsTheSamplePrefab()
        {
            var installer = AssetDatabase.LoadAssetAtPath<PopupInstaller>(
                "Assets/Features/_Services/Popup/FeatureAssets/Public/PopupInstaller.asset");
            var so = new SerializedObject(installer);

            Assert.That(so.FindProperty("cheat").enumValueIndex, Is.EqualTo((int)PopupCheatState.None));
            var config = (PopupConfig)so.FindProperty("config").objectReferenceValue;
            Assert.That(config.DevSamplePrefab, Is.Not.Null, "PopupConfig.devSamplePrefab");
            Assert.That(config.DevSamplePrefab.GetComponent<IDevSampleView>(), Is.Not.Null);
            Assert.That(config.DevSamplePrefab.GetComponent<RectTransform>(), Is.Not.Null, "a popup must be UI");
        }

        // ---- helpers

        private sealed class ScenePrefabs : Dictionary<PopupId, GameObject>, IPopupPrefabs
        {
            public void Prepare() { }

            public bool TryGet(PopupId id, out GameObject prefab) => TryGetValue(id, out prefab);
        }

        private sealed class Presenter : IPopupPresenter
        {
            private readonly List<Object> created;

            internal Presenter(List<Object> created) => this.created = created;

            public GameObject Layer => null;

            public GameObject Present(GameObject prefab)
            {
                var instance = Object.Instantiate(prefab);
                created.Add(instance);
                return instance;
            }

            public void Dismiss(GameObject instance) => Object.DestroyImmediate(instance);

            public void DismissAll() { }
        }

        private sealed class Log : IPopupLog
        {
            internal readonly List<string> Errors = new List<string>();

            public void Warning(string message) => Errors.Add(message);

            public void Error(string message) => Errors.Add(message);
        }
    }

    /// <summary>Stands for the sample popup's view: a click the test can press.</summary>
    internal sealed class FakeSampleView : MonoBehaviour, IDevSampleView
    {
        private Action closeClicked;

        public event Action CloseClicked
        {
            add => closeClicked += value;
            remove => closeClicked -= value;
        }

        internal int Listeners => closeClicked?.GetInvocationList().Length ?? 0;

        internal void Click() => closeClicked?.Invoke();
    }
}
