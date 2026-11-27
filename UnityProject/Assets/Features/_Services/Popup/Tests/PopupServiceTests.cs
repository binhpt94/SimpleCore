using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using SessionState = Game.Core.SessionState;

namespace Game.Services.Popup.Tests
{
    public class PopupServiceTests
    {
        private static readonly PopupId IdA = new PopupId("Test/A");
        private static readonly PopupId IdB = new PopupId("Test/B");
        private static readonly PopupId IdC = new PopupId("Test/C");
        private static readonly PopupId IdD = new PopupId("Test/D");

        private readonly List<Object> created = new List<Object>();
        private FakePresenter presenter;
        private FakePrefabs prefabs;
        private FakeLog log;
        private PopupService service;

        [SetUp]
        public void SetUp()
        {
            presenter = new FakePresenter(created);
            prefabs = new FakePrefabs { [IdA] = Make("A"), [IdB] = Make("B"), [IdC] = Make("C"), [IdD] = Make("D") };
            log = new FakeLog();
            service = new PopupService(presenter, prefabs, log, 3);
            service.Start();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        // ---- stack

        [Test]
        public void Show_OpensThePopupOnTopAtOnce()
        {
            var handle = service.Show(IdA);

            Assert.That(handle.IsValid, Is.True);
            Assert.That(presenter.Open, Has.Count.EqualTo(1));
            Assert.That(service.IsOnTop(handle), Is.True);
        }

        [Test]
        public void Show_WhileAnotherIsOpen_StacksTheNewOneOnTop()
        {
            var first = service.Show(IdA);
            var second = service.Show(IdB);

            Assert.That(service.OpenCount, Is.EqualTo(2));
            Assert.That(service.IsOnTop(second), Is.True);
            Assert.That(service.IsOnTop(first), Is.False);
        }

        [Test]
        public void Close_TopPopup_GivesTopBackToThePopupBelow()
        {
            var first = service.Show(IdA);
            var second = service.Show(IdB);

            service.Close(second);

            Assert.That(service.IsOnTop(first), Is.True);
            Assert.That(presenter.Open, Has.Count.EqualTo(1));
        }

        [Test]
        public void Close_PopupInTheMiddle_LeavesTheTopOneOnTop()
        {
            service.Show(IdA);
            var middle = service.Show(IdB);
            var top = service.Show(IdC);

            service.Close(middle);

            Assert.That(service.OpenCount, Is.EqualTo(2));
            Assert.That(service.IsOnTop(top), Is.True);
            Assert.That(presenter.Dismissed, Has.Count.EqualTo(1));
        }

        [Test]
        public void Close_ClosedUnknownOrDefaultHandle_IsIgnored()
        {
            var first = service.Show(IdA);
            service.Close(first);

            service.Close(first);
            service.Close(default);
            service.Close(new PopupHandle(999));

            Assert.That(presenter.Dismissed, Has.Count.EqualTo(1));
            Assert.That(log.Count, Is.EqualTo(0));
        }

        [Test]
        public void Show_FourthPopup_IsRefusedWithAnErrorNamingTheId_AndTheThreeStay()
        {
            service.Show(IdA);
            service.Show(IdB);
            var top = service.Show(IdC);

            var fourth = service.Show(IdD);

            Assert.That(fourth.IsValid, Is.False);
            Assert.That(service.OpenCount, Is.EqualTo(3));
            Assert.That(service.IsOnTop(top), Is.True);
            Assert.That(log.Errors, Has.Count.EqualTo(1).And.Some.Contains("Test/D"));
        }

        [Test]
        public void Show_IdThatIsAlreadyOpen_IsRefusedWithAWarning_AndDoesNotComeToTheTop()
        {
            var first = service.Show(IdA);
            var second = service.Show(IdB);

            var again = service.Show(IdA);

            Assert.That(again.IsValid, Is.False);
            Assert.That(service.OpenCount, Is.EqualTo(2));
            Assert.That(service.IsOnTop(second), Is.True);
            Assert.That(service.IsOnTop(first), Is.False);
            Assert.That(log.Warnings, Has.Count.EqualTo(1).And.Some.Contains("Test/A"));
        }

        [Test]
        public void Show_IdClosedEarlier_OpensAgain()
        {
            service.Close(service.Show(IdA));

            Assert.That(service.Show(IdA).IsValid, Is.True);
        }

        [Test]
        public void Show_IdTheSceneDoesNotHave_ShowsNothingAndLogsAnErrorNamingTheId()
        {
            var open = service.Show(IdA);

            var missing = service.Show(new PopupId("Test/Missing"));

            Assert.That(missing.IsValid, Is.False);
            Assert.That(presenter.Open, Has.Count.EqualTo(1));
            Assert.That(service.IsOnTop(open), Is.True);
            Assert.That(log.Errors, Has.Count.EqualTo(1).And.Some.Contains("Test/Missing"));
        }

        [Test]
        public void Show_DefaultId_IsAProgrammingErrorAndThrows()
        {
            Assert.Throws<ArgumentException>(() => service.Show(default));
            Assert.That(presenter.Open, Is.Empty);
        }

        [Test]
        public void Popups_OpenTakeTheTopAndClose_WhileTheBattleClockIsStopped()
        {
            var timeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;   // the battle stands still (paused or lost)

                var first = service.Show(IdA);
                var second = service.Show(IdB);
                Assert.That(service.IsOnTop(second), Is.True);

                service.Close(second);

                Assert.That(service.IsOnTop(first), Is.True);
                Assert.That(presenter.Open, Has.Count.EqualTo(1));
                Assert.That(log.Count, Is.EqualTo(0));
            }
            finally
            {
                Time.timeScale = timeScale;
            }
        }

        [Test]
        public void RunEndsWhilePausePopupIsOpen_CallerClosesItThenOpensTheLosePopup_InTheSameFrame()
        {
            var timeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;   // should not happen (battle stands still), but if it does:
                var pause = service.Show(IdA);

                service.Close(pause);
                var lose = service.Show(IdB);

                Assert.That(lose.IsValid, Is.True, "the lose popup opens at once");
                Assert.That(service.IsOnTop(lose), Is.True);
                Assert.That(service.OpenCount, Is.EqualTo(1), "the pause popup and its backdrop are gone");
                Assert.That(service.IsOnTop(pause), Is.False);
                Assert.That(presenter.Dismissed, Has.Count.EqualTo(1));
                Assert.That(log.Count, Is.EqualTo(0));

                service.Close(pause);   // a late close from the pause side is ignored

                Assert.That(service.IsOnTop(lose), Is.True);
                Assert.That(log.Count, Is.EqualTo(0));
            }
            finally
            {
                Time.timeScale = timeScale;
            }
        }

        [Test]
        public void OpenIds_ListsTheOpenPopupsBottomFirst_AndIsEmptyOnceTheyClose()
        {
            IPopupStack stackView = service;
            Assert.That(stackView.OpenIds, Is.Empty);

            var first = service.Show(IdA);
            var second = service.Show(IdB);
            var snapshot = stackView.OpenIds;

            Assert.That(snapshot, Is.EqualTo(new[] { IdA, IdB }));

            service.Close(first);
            Assert.That(stackView.OpenIds, Is.EqualTo(new[] { IdB }));
            service.Close(second);

            Assert.That(stackView.OpenIds, Is.Empty);
            Assert.That(snapshot, Is.EqualTo(new[] { IdA, IdB }), "a list already handed out does not change");
        }

        [Test]
        public void NoPopupOpen_NothingIsOnTop_AndNothingWasPresented()
        {
            Assert.That(service.OpenCount, Is.EqualTo(0));
            Assert.That(service.Top.IsValid, Is.False);
            Assert.That(service.IsOnTop(default), Is.False);
            Assert.That(presenter.Open, Is.Empty);
        }

        // ---- views

        [Test]
        public void TryGetView_FindsTheCallersComponentOnTheOpenPopup()
        {
            prefabs[IdA].AddComponent<BoxCollider>();   // stands for the caller's view component
            var handle = service.Show(IdA);

            Assert.That(service.TryGetView<BoxCollider>(handle, out var view), Is.True);
            Assert.That(view.gameObject, Is.SameAs(presenter.Open[0]));
        }

        [Test]
        public void TryGetView_ClosedPopupOrMissingComponent_IsFalse()
        {
            var handle = service.Show(IdA);
            Assert.That(service.TryGetView<BoxCollider>(handle, out _), Is.False);

            service.Close(handle);
            Assert.That(service.TryGetView<Transform>(handle, out _), Is.False);
        }

        // ---- session

        [Test]
        public void Show_BeforeTheSceneRuns_IsRefusedWithAnError()
        {
            var notRunning = new PopupService(presenter, prefabs, log, 3);

            Assert.That(notRunning.Show(IdA).IsValid, Is.False);
            Assert.That(presenter.Open, Is.Empty);
            Assert.That(log.Errors, Has.Count.EqualTo(1).And.Some.Contains("Test/A"));
        }

        [Test]
        public void SessionEnds_EveryPopupGoes_AndNoneOpensAfterwards()
        {
            var handle = service.Show(IdA);
            service.Show(IdB);

            service.Stop();

            Assert.That(presenter.DismissedAll, Is.True);
            Assert.That(service.OpenCount, Is.EqualTo(0));
            Assert.That(service.IsOnTop(handle), Is.False);
            Assert.That(service.Show(IdC).IsValid, Is.False);
        }

        [Test]
        public void Participant_LoadsByPreparingThePopups_RunsThenDisposes()
        {
            var fresh = new PopupService(presenter, prefabs, log, 3);
            var participant = new PopupParticipant(fresh, prefabs);

            participant.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            Assert.That(prefabs.Prepared, Is.True);
            Assert.That(fresh.Show(IdA).IsValid, Is.False, "not running before Run");

            participant.Run();
            Assert.That(fresh.Show(IdB).IsValid, Is.True);

            participant.DisposeAsync().GetAwaiter().GetResult();
            Assert.That(fresh.OpenCount, Is.EqualTo(0));
        }

        [Test]
        public void SessionUsage_OpenPopupsCountAsHeldViews_AndTheCountIsZeroAfterTheSessionIsDisposed()
        {
            var fresh = new PopupService(presenter, prefabs, log, 3);
            var participant = new PopupParticipant(fresh, prefabs);
            ISessionParticipantUsage usage = participant;
            participant.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            participant.Run();

            Assert.That(usage.HeldViews, Is.EqualTo(0), "no layer before the first popup");

            fresh.Show(IdA);
            fresh.Show(IdB);
            Assert.That(usage.HeldViews, Is.EqualTo(3), "the popup layer and each open popup are views the session holds");

            participant.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(usage.HeldViews, Is.EqualTo(0));
            Assert.That(usage.RunningJobs, Is.EqualTo(0), "the popup service runs no timed job");
        }

        [Test]
        public void SessionUsage_OpenThenCloseEveryPopupThenDispose_CountsAreZero()
        {
            var fresh = new PopupService(presenter, prefabs, log, 3);
            var participant = new PopupParticipant(fresh, prefabs);
            ISessionParticipantUsage usage = participant;
            participant.Run();

            fresh.Close(fresh.Show(IdA));
            Assert.That(usage.HeldViews, Is.EqualTo(1), "the layer stays for the next popup until the session ends");

            participant.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(usage.HeldViews, Is.EqualTo(0));
            Assert.That(usage.RunningJobs, Is.EqualTo(0));
        }

        [Test]
        public void SessionUsage_ClosingAPopup_StopsCountingItAsAHeldView()
        {
            var fresh = new PopupService(presenter, prefabs, log, 3);
            var participant = new PopupParticipant(fresh, prefabs);
            ISessionParticipantUsage usage = participant;
            participant.Run();

            var first = fresh.Show(IdA);
            fresh.Show(IdB);
            fresh.Close(first);

            Assert.That(usage.HeldViews, Is.EqualTo(2), "the layer and the popup still open");
        }

        // ---- catalog

        [Test]
        public void CatalogPrefabs_FindsListedIds_AndNotUnknownOrDefaultOnes()
        {
            var lookup = new CatalogPopupPrefabs(Catalog("GamePopups", ("Test/A", prefabs[IdA])));

            Assert.That(lookup.TryGet(IdA, out var found), Is.True);
            Assert.That(found, Is.SameAs(prefabs[IdA]));
            Assert.That(lookup.TryGet(IdB, out _), Is.False);
            Assert.That(lookup.TryGet(default, out _), Is.False);
        }

        [Test]
        public void CatalogPrefabs_Prepare_FailsNamingTheId_ForABadIdANonPrefabOrADuplicate()
        {
            var badId = new CatalogPopupPrefabs(Catalog("GamePopups", ("not an id", prefabs[IdA])));
            var notPrefab = new CatalogPopupPrefabs(Catalog("GamePopups", ("Test/A", Catalog("Other"))));
            var twice = new CatalogPopupPrefabs(Catalog("GamePopups", ("Test/A", prefabs[IdA]), ("Test/A", prefabs[IdB])));

            Assert.That(Assert.Throws<InvalidOperationException>(badId.Prepare).Message, Does.Contain("not an id"));
            Assert.That(Assert.Throws<InvalidOperationException>(notPrefab.Prepare).Message, Does.Contain("Test/A"));
            Assert.That(Assert.Throws<InvalidOperationException>(twice.Prepare).Message, Does.Contain("Test/A"));
        }

        [Test]
        public void CatalogPrefabs_Prepare_IdListedWithoutItsAsset_FailsTheLoadNamingTheId()
        {
            var missing = new CatalogPopupPrefabs(Catalog("GamePopups", ("Test/A", prefabs[IdA]), ("Test/B", null)));

            var error = Assert.Throws<InvalidOperationException>(missing.Prepare);

            Assert.That(error.Message, Does.Contain("Test/B"));
        }

        [Test]
        public void Participant_LoadFails_WhenAPopupOfTheSceneHasNoAsset()
        {
            var lookup = new CatalogPopupPrefabs(Catalog("GamePopups", ("Test/B", null)));
            var participant = new PopupParticipant(new PopupService(presenter, lookup, log, 3), lookup);

            Assert.Throws<InvalidOperationException>(
                () => participant.LoadAsync(CancellationToken.None).GetAwaiter().GetResult());
        }

        [Test]
        public void CatalogPrefabs_Prepare_GoodCatalog_Passes()
        {
            var lookup = new CatalogPopupPrefabs(Catalog("GamePopups", ("Test/A", prefabs[IdA]), ("Test/B", prefabs[IdB])));

            Assert.DoesNotThrow(lookup.Prepare);
        }

        // ---- installer

        [Test]
        public void InstallerAsset_RegistersBothContractsAndJoinsTheSession()
        {
            var registry = new ServiceRegistry();
            var session = new FakeSession();
            registry.Register<IAssetCatalogs>(new AssetCatalogs(new[] { Catalog("GamePopups", ("Test/A", prefabs[IdA])) }));
            registry.Register<ISessionScene>(session);
            registry.Register<ICheatSelection>(new FakeCheatSelection());

            LoadInstallerAsset().Install(registry);

            Assert.That(registry.TryResolve<IPopupService>(out var popups), Is.True);
            Assert.That(registry.TryResolve<IPopupViews>(out var views), Is.True);
            Assert.That(views, Is.SameAs(popups));
            Assert.That(registry.TryResolve<IPopupStack>(out var stackView), Is.True, "the read-only stack is registered too");
            Assert.That(stackView, Is.SameAs(popups));
            Assert.That(session.Joined, Has.Count.EqualTo(1));
        }

        [Test]
        public void InstallerAsset_SceneWithoutPopupCatalog_FailsNamingTheKind()
        {
            var registry = new ServiceRegistry();
            registry.Register<IAssetCatalogs>(new AssetCatalogs(new AssetCatalog[0]));
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<ICheatSelection>(new FakeCheatSelection());

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstallerAsset().Install(registry));

            Assert.That(error.Message, Does.Contain("'Popup'"));
        }

        [Test]
        public void InstallerAsset_WithoutSession_Fails()
        {
            var registry = new ServiceRegistry();
            registry.Register<IAssetCatalogs>(new AssetCatalogs(new[] { Catalog("GamePopups") }));
            registry.Register<ICheatSelection>(new FakeCheatSelection());

            Assert.That(() => LoadInstallerAsset().Install(registry), Throws.Exception);
        }

        // ---- PopupId

        [TestCase("Shop/Purchased")]
        [TestCase("PopupLose/Main")]
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

        // ---- helpers

        internal static PopupInstaller LoadInstallerAsset()
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

        private AssetCatalog Catalog(string name, params (string id, Object asset)[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<AssetCatalog>();
            catalog.name = name;
            created.Add(catalog);
            var so = new SerializedObject(catalog);
            so.FindProperty("kind").stringValue = CatalogPopupPrefabs.Kind;
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
            internal bool Prepared;

            public void Prepare() => Prepared = true;

            public bool TryGet(PopupId id, out GameObject prefab) => TryGetValue(id, out prefab);
        }

        /// <summary>Clones the prefab so the test can look for components on the "instance".</summary>
        private sealed class FakePresenter : IPopupPresenter
        {
            private readonly List<Object> created;
            internal readonly List<GameObject> Open = new List<GameObject>();
            internal readonly List<GameObject> Dismissed = new List<GameObject>();
            internal bool DismissedAll;

            internal FakePresenter(List<Object> created) => this.created = created;

            public GameObject Layer { get; private set; }

            public GameObject Present(GameObject prefab)
            {
                if (Layer == null)
                {
                    Layer = new GameObject("FakePopupLayer");
                    created.Add(Layer);
                }
                var instance = Object.Instantiate(prefab);
                created.Add(instance);
                Open.Add(instance);
                return instance;
            }

            public void Dismiss(GameObject instance)
            {
                Open.Remove(instance);
                Dismissed.Add(instance);
            }

            public void DismissAll()
            {
                Open.Clear();
                Layer = null;
                DismissedAll = true;
            }
        }

        private sealed class FakeLog : IPopupLog
        {
            internal readonly List<string> Warnings = new List<string>();
            internal readonly List<string> Errors = new List<string>();
            internal int Count => Warnings.Count + Errors.Count;

            public void Warning(string message) => Warnings.Add(message);

            public void Error(string message) => Errors.Add(message);
        }

        internal sealed class FakeSession : ISessionScene
        {
            internal readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();

            public SessionState State => SessionState.Init;

            public event Action<SessionState> StateChanged { add { } remove { } }

            public void Join(ISessionParticipant participant) => Joined.Add(participant);

            public UniTask EndAsync() => UniTask.CompletedTask;
        }
    }
}
