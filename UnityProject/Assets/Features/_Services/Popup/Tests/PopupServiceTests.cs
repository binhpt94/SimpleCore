using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Services.Popup.Tests
{
    public class PopupServiceTests
    {
        private FakePresenter presenter;
        private PopupService service;
        private GameObject prefabA;
        private GameObject prefabB;
        private GameObject prefabC;

        [SetUp]
        public void SetUp()
        {
            presenter = new FakePresenter();
            service = new PopupService(presenter);
            prefabA = new GameObject("A");
            prefabB = new GameObject("B");
            prefabC = new GameObject("C");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(prefabA);
            Object.DestroyImmediate(prefabB);
            Object.DestroyImmediate(prefabC);
        }

        [Test]
        public void Show_WhenNothingOpen_OpensImmediately()
        {
            var handle = service.Show(new PopupRequest(prefabA));

            Assert.That(handle.IsValid, Is.True);
            Assert.That(service.Current, Is.EqualTo(handle));
            Assert.That(presenter.Presented, Is.EqualTo(new[] { prefabA }));
        }

        [Test]
        public void Show_WhileAnotherIsOpen_Waits()
        {
            var first = service.Show(new PopupRequest(prefabA));
            service.Show(new PopupRequest(prefabB));

            Assert.That(service.Current, Is.EqualTo(first));
            Assert.That(service.WaitingCount, Is.EqualTo(1));
            Assert.That(presenter.Presented, Is.EqualTo(new[] { prefabA }));
        }

        [Test]
        public void Close_OpenPopup_DismissesItAndOpensNextInRequestOrder()
        {
            var first = service.Show(new PopupRequest(prefabA));
            var second = service.Show(new PopupRequest(prefabB));
            service.Show(new PopupRequest(prefabC));

            service.Close(first);

            Assert.That(presenter.Dismissed, Is.EqualTo(new[] { prefabA }));
            Assert.That(service.Current, Is.EqualTo(second));
            Assert.That(presenter.Presented, Is.EqualTo(new[] { prefabA, prefabB }));
            Assert.That(service.WaitingCount, Is.EqualTo(1));
        }

        [Test]
        public void Close_LastPopup_LeavesNothingOpen()
        {
            var only = service.Show(new PopupRequest(prefabA));

            service.Close(only);

            Assert.That(service.Current.IsValid, Is.False);
            Assert.That(service.WaitingCount, Is.EqualTo(0));
        }

        [Test]
        public void Close_WaitingPopup_CancelsItWithoutTouchingTheOpenOne()
        {
            var first = service.Show(new PopupRequest(prefabA));
            var second = service.Show(new PopupRequest(prefabB));

            service.Close(second);

            Assert.That(service.Current, Is.EqualTo(first));
            Assert.That(service.WaitingCount, Is.EqualTo(0));
            Assert.That(presenter.Dismissed, Is.Empty);
        }

        [Test]
        public void Close_SameHandleTwice_SecondCallDoesNothing()
        {
            var first = service.Show(new PopupRequest(prefabA));
            var second = service.Show(new PopupRequest(prefabB));

            service.Close(first);
            service.Close(first);

            Assert.That(service.Current, Is.EqualTo(second));
            Assert.That(presenter.Dismissed, Has.Count.EqualTo(1));
        }

        [Test]
        public void Show_WithoutPrefab_ReturnsInvalidHandleAndShowsNothing()
        {
            Assert.That(service.Show(new PopupRequest(null)).IsValid, Is.False);
            Assert.That(service.Show(null).IsValid, Is.False);
            Assert.That(presenter.Presented, Is.Empty);
        }

        [Test]
        public void Close_InvalidOrUnknownHandle_IsIgnored()
        {
            var first = service.Show(new PopupRequest(prefabA));

            service.Close(default);
            service.Close(new PopupHandle(999));

            Assert.That(service.Current, Is.EqualTo(first));
            Assert.That(presenter.Dismissed, Is.Empty);
        }

        [Test]
        public void InstallerAsset_RegistersPopupService()
        {
            var installer = AssetDatabase.LoadAssetAtPath<PopupInstaller>(
                "Assets/Features/_Services/Popup/FeatureAssets/Public/PopupInstaller.asset");
            Assert.That(installer, Is.Not.Null, "PopupInstaller.asset missing or its script reference is broken");
            var registry = new ServiceRegistry();

            installer.Install(registry);

            Assert.That(registry.TryResolve<IPopupService>(out _), Is.True);
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
