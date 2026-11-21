using System;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Services.Popup.Tests
{
    public class PopupServiceTests
    {
        private static readonly PopupId IdA = new PopupId("Test/A");
        private static readonly PopupId IdB = new PopupId("Test/B");
        private static readonly PopupId IdC = new PopupId("Test/C");

        private readonly List<Object> created = new List<Object>();
        private FakePresenter presenter;
        private FakePrefabs prefabs;
        private PopupService service;
        private GameObject prefabA;
        private GameObject prefabB;
        private GameObject prefabC;

        [SetUp]
        public void SetUp()
        {
            presenter = new FakePresenter();
            prefabA = Make("A");
            prefabB = Make("B");
            prefabC = Make("C");
            prefabs = new FakePrefabs { [IdA] = prefabA, [IdB] = prefabB, [IdC] = prefabC };
            service = new PopupService(presenter, prefabs);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Show_WhenNothingOpen_OpensThePrefabOfThatIdImmediately()
        {
            var handle = service.Show(IdA);

            Assert.That(handle.IsValid, Is.True);
            Assert.That(service.Current, Is.EqualTo(handle));
            Assert.That(presenter.Presented, Is.EqualTo(new[] { prefabA }));
        }

        [Test]
        public void Show_WhileAnotherIsOpen_Waits()
        {
            var first = service.Show(IdA);
            service.Show(IdB);

            Assert.That(service.Current, Is.EqualTo(first));
            Assert.That(service.WaitingCount, Is.EqualTo(1));
            Assert.That(presenter.Presented, Is.EqualTo(new[] { prefabA }));
        }

        [Test]
        public void Close_OpenPopup_DismissesItAndOpensNextInRequestOrder()
        {
            var first = service.Show(IdA);
            var second = service.Show(IdB);
            service.Show(IdC);

            service.Close(first);

            Assert.That(presenter.Dismissed, Is.EqualTo(new[] { prefabA }));
            Assert.That(service.Current, Is.EqualTo(second));
            Assert.That(presenter.Presented, Is.EqualTo(new[] { prefabA, prefabB }));
            Assert.That(service.WaitingCount, Is.EqualTo(1));
        }

        [Test]
        public void Close_LastPopup_LeavesNothingOpen()
        {
            var only = service.Show(IdA);

            service.Close(only);

            Assert.That(service.Current.IsValid, Is.False);
            Assert.That(service.WaitingCount, Is.EqualTo(0));
        }

        [Test]
        public void Close_WaitingPopup_CancelsItWithoutTouchingTheOpenOne()
        {
            var first = service.Show(IdA);
            var second = service.Show(IdB);

            service.Close(second);

            Assert.That(service.Current, Is.EqualTo(first));
            Assert.That(service.WaitingCount, Is.EqualTo(0));
            Assert.That(presenter.Dismissed, Is.Empty);
        }

        [Test]
        public void Close_SameHandleTwice_SecondCallDoesNothing()
        {
            var first = service.Show(IdA);
            var second = service.Show(IdB);

            service.Close(first);
            service.Close(first);

            Assert.That(service.Current, Is.EqualTo(second));
            Assert.That(presenter.Dismissed, Has.Count.EqualTo(1));
        }

        [Test]
        public void Show_IdTheSceneDoesNotHave_FailsAndShowsNothing()
        {
            Assert.Throws<InvalidOperationException>(() => service.Show(new PopupId("Test/Missing")));
            Assert.That(presenter.Presented, Is.Empty);
            Assert.That(service.Current.IsValid, Is.False);
        }

        [Test]
        public void Close_InvalidOrUnknownHandle_IsIgnored()
        {
            var first = service.Show(IdA);

            service.Close(default);
            service.Close(new PopupHandle(999));

            Assert.That(service.Current, Is.EqualTo(first));
            Assert.That(presenter.Dismissed, Is.Empty);
        }

        [TestCase("Shop/Purchased")]
        [TestCase("Shop/Buy.Confirm")]
        [TestCase("_Services/Popup/Error")]
        public void PopupId_OwnerSlashName_IsAccepted(string value)
        {
            Assert.That(new PopupId(value).Value, Is.EqualTo(value));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Purchased")]
        [TestCase("Shop/purchased")]
        [TestCase("Shop/A/B")]
        [TestCase("Popup/Services/X ")]
        public void PopupId_OtherText_IsRefused(string value)
        {
            Assert.Throws<ArgumentException>(() => new PopupId(value));
        }

        [Test]
        public void CatalogPrefabs_DefaultOrUnknownId_FailsNamingIdAndCatalog()
        {
            var lookup = new CatalogPopupPrefabs(Catalog("GamePopups", "Popup", ("Test/A", prefabA)));

            Assert.That(lookup.Get(IdA), Is.SameAs(prefabA));
            Assert.Throws<ArgumentException>(() => lookup.Get(default));
            var error = Assert.Throws<InvalidOperationException>(() => lookup.Get(IdB));
            Assert.That(error.Message, Does.Contain("Test/B").And.Contain("GamePopups"));
        }

        [Test]
        public void CatalogPrefabs_EntryThatIsNotAPopupIdOrNotAPrefab_Fails()
        {
            Assert.Throws<InvalidOperationException>(
                () => new CatalogPopupPrefabs(Catalog("GamePopups", "Popup", ("not an id", prefabA))));
            Assert.Throws<InvalidOperationException>(
                () => new CatalogPopupPrefabs(Catalog("GamePopups", "Popup", ("Test/A", Catalog("Other", "X")))));
        }

        [Test]
        public void InstallerAsset_RegistersPopupServiceReadingTheScenesPopupCatalog()
        {
            var registry = new ServiceRegistry();
            registry.Register<IAssetCatalogs>(new AssetCatalogs(new[] { Catalog("GamePopups", "Popup", ("Test/A", prefabA)) }));

            LoadInstallerAsset().Install(registry);

            Assert.That(registry.TryResolve<IPopupService>(out _), Is.True);
        }

        [Test]
        public void InstallerAsset_SceneWithoutPopupCatalog_FailsNamingTheKind()
        {
            var registry = new ServiceRegistry();
            registry.Register<IAssetCatalogs>(new AssetCatalogs(new AssetCatalog[0]));

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstallerAsset().Install(registry));

            Assert.That(error.Message, Does.Contain("'Popup'"));
        }

        private static PopupInstaller LoadInstallerAsset()
        {
            var installer = AssetDatabase.LoadAssetAtPath<PopupInstaller>(
                "Assets/Features/_Services/Popup/FeatureAssets/Public/PopupInstaller.asset");
            Assert.That(installer, Is.Not.Null, "PopupInstaller.asset missing or its script reference is broken");
            return installer;
        }

        private GameObject Make(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }

        private AssetCatalog Catalog(string name, string kind, params (string id, Object asset)[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<AssetCatalog>();
            catalog.name = name;
            created.Add(catalog);
            var so = new SerializedObject(catalog);
            so.FindProperty("kind").stringValue = kind;
            var list = so.FindProperty("entries");
            list.arraySize = entries.Length;
            for (var i = 0; i < entries.Length; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue = entries[i].id;
                list.GetArrayElementAtIndex(i).FindPropertyRelative("asset").objectReferenceValue = entries[i].asset;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private sealed class FakePrefabs : Dictionary<PopupId, GameObject>, IPopupPrefabs
        {
            public GameObject Get(PopupId id)
            {
                if (TryGetValue(id, out var prefab)) return prefab;
                throw new InvalidOperationException($"no prefab for {id}");
            }
        }

        /// <summary>Records calls; "instances" are the prefabs themselves so the test can compare them.</summary>
        private sealed class FakePresenter : IPopupPresenter
        {
            internal readonly List<GameObject> Presented = new List<GameObject>();
            internal readonly List<GameObject> Dismissed = new List<GameObject>();

            public GameObject Present(GameObject prefab)
            {
                Presented.Add(prefab);
                return prefab;
            }

            public void Dismiss(GameObject instance)
            {
                Dismissed.Add(instance);
            }
        }
    }
}
