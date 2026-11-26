using System;
using System.Collections.Generic;
using Game.Ads;
using Game.Core;
using Game.Run;
using Game.Services.Popup;
using Game.SimClock;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SessionState = Game.Core.SessionState;

namespace Game.PopupRevive.Tests
{
    /// <summary>The real assets in FeatureAssets/Public: installer and the PopupRevive/Main prefab.</summary>
    public class PopupReviveAssetTests
    {
        private const string InstallerPath = "Assets/Features/PopupRevive/FeatureAssets/Public/PopupReviveInstaller.asset";
        private const string PrefabPath = "Assets/Features/PopupRevive/FeatureAssets/Public/PopupReviveMain.prefab";

        [Test]
        public void InstallerAssetJoinsTheSessionWithEveryDependencyPresent()
        {
            var installer = AssetDatabase.LoadAssetAtPath<PopupReviveInstaller>(InstallerPath);
            Assert.IsNotNull(installer, InstallerPath);

            var session = new FakeSession();
            var registry = new Registry();
            registry.Register<ISessionScene>(session);
            var run = new NullRun();
            registry.Register<IRunFlow>(run);
            registry.Register<IRunRevive>(run);
            registry.Register<IRewardedAds>(new NullAds());
            var popups = new NullPopups();
            registry.Register<IPopupService>(popups);
            registry.Register<IPopupViews>(popups);
            registry.Register<ISimClock>(new NullClock());

            installer.Install(registry);

            Assert.AreEqual(1, session.Joined.Count);
        }

        // revive-spec#watch-button: same row y=540 in the 800x700 panel, 400 and 260 wide, 120 tall.

        [Test]
        public void WatchButtonIs400WideAtX50Y540()
        {
            AssertBox("Watch", 50, 540, 400, 120);
        }

        [Test]
        public void EndButtonIs260WideAtX490Y540()
        {
            AssertBox("End", 490, 540, 260, 120);
        }

        [Test]
        public void TitleGainAndWaitLinesSitAtTheirRows()
        {
            AssertBox("Title", 50, 40, 700, 90);
            AssertBox("Gain", 50, 160, 700, 70);
            AssertBox("Waiting", 50, 340, 700, 70);
        }

        [Test]
        public void PanelIs800By700()
        {
            var panel = Find("Panel");
            Assert.AreEqual(new Vector2(800, 700), panel.rect.size);
        }

        // revive-spec#stage-prototype: PROTOTYPE has all words, both buttons and the wait line; while waiting the
        // buttons are dimmed by changing their opacity.

        [Test]
        public void PrototypePrefabHasTitleGainWaitLineAndBothButtons()
        {
            foreach (var name in new[] { "Title", "Gain", "Waiting", "Watch", "End", "WatchLabel", "EndLabel" })
                Assert.IsNotNull(Find(name), name);
        }

        [Test]
        public void PrototypeWaitingDimsBothButtonsByOpacityAndChoosingShowsThemFull()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, PrefabPath);
            var popup = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var view = popup.GetComponent<IRevivePopupView>();
                Assert.IsNotNull(view, "root has the revive popup view");
                var group = FindIn(popup, "Buttons").GetComponent<CanvasGroup>();
                Assert.IsNotNull(group, "Buttons has a CanvasGroup");
                Assert.IsTrue(FindIn(popup, "Watch").IsChildOf(group.transform), "Watch dims with Buttons");
                Assert.IsTrue(FindIn(popup, "End").IsChildOf(group.transform), "End dims with Buttons");

                view.SetWaiting(true);
                Assert.Less(group.alpha, 1f, "dimmed while waiting");
                Assert.Greater(group.alpha, 0f, "still visible while waiting");
                Assert.IsFalse(group.interactable);
                Assert.IsFalse(group.blocksRaycasts);
                Assert.IsTrue(FindIn(popup, "Waiting").gameObject.activeSelf, "wait line shown");

                view.SetWaiting(false);
                Assert.AreEqual(1f, group.alpha, "full opacity while choosing");
                Assert.IsTrue(group.interactable);
                Assert.IsFalse(FindIn(popup, "Waiting").gameObject.activeSelf, "wait line hidden");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(popup);
            }
        }

        private static Transform FindIn(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            Assert.Fail($"{root.name} has no child named {name}.");
            return null;
        }

        private static void AssertBox(string name, float x, float y, float w, float h)
        {
            var panel = Find("Panel");
            var box = Find(name);
            var corners = new Vector3[4];
            box.GetWorldCorners(corners);
            var panelCorners = new Vector3[4];
            panel.GetWorldCorners(panelCorners);
            // Corner 1 is top-left. Frame y grows downwards from the panel's top edge.
            var left = corners[1].x - panelCorners[1].x;
            var top = panelCorners[1].y - corners[1].y;
            Assert.AreEqual(x, left, 0.5f, name + " x");
            Assert.AreEqual(y, top, 0.5f, name + " y");
            Assert.AreEqual(w, box.rect.width, 0.5f, name + " width");
            Assert.AreEqual(h, box.rect.height, 0.5f, name + " height");
        }

        private static RectTransform Find(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, PrefabPath);
            foreach (var rect in prefab.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == name) return rect;
            Assert.Fail($"{PrefabPath} has no child named {name}.");
            return null;
        }

        private sealed class Registry : IServiceRegistry
        {
            private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

            public void Register<T>(T service) where T : class => _services[typeof(T)] = service;

            public T Resolve<T>() where T : class => (T)_services[typeof(T)];

            public bool TryResolve<T>(out T service) where T : class
            {
                service = _services.TryGetValue(typeof(T), out var found) ? (T)found : null;
                return service != null;
            }
        }

        private sealed class FakeSession : ISessionScene
        {
            public List<ISessionParticipant> Joined { get; } = new List<ISessionParticipant>();
            public SessionState State => SessionState.Init;
            public event Action<SessionState> StateChanged { add { } remove { } }
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public Cysharp.Threading.Tasks.UniTask EndAsync() => Cysharp.Threading.Tasks.UniTask.CompletedTask;
        }

        private sealed class NullRun : IRunFlow, IRunRevive
        {
            public RunStep Step => RunStep.Home;
            public RunTeam Team => default;
            public bool IsPaused => false;
            public bool LostAfterFailedRevive => false;
            public event Action<RunStep> StepChanged { add { } remove { } }
            public event Action<RunTeam> RunStarted { add { } remove { } }
            public event Action BattleStarted { add { } remove { } }
            public event Action BattleStopped { add { } remove { } }
            public event Action BattleCleared { add { } remove { } }
            public event Action ReviveStarted { add { } remove { } }
            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Pause() { }
            public void Resume() { }
            public void Abandon() { }
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
            public void Revive() { }
            public void DeclineRevive() { }
            public void FailRevive() { }
            public void ReviveShown() { }
        }

        private sealed class NullAds : IRewardedAds
        {
            public void Show(int offerId, Action<AdReply> onReply) { }
            public void Cancel(int offerId) { }
        }

        private sealed class NullClock : ISimClock
        {
            public SimMode Mode => SimMode.FixedTick;
            public float StepSeconds => 0.025f;
            public long StepCount => 0;
            public long GameplayStepCount => 0;
            public double GameplaySeconds => 0;
            public IDisposable OnStep(string owner, Action<SimStep> onStep) => new Nothing();
            public void Enqueue(string owner, Action apply) { }

            private sealed class Nothing : IDisposable
            {
                public void Dispose() { }
            }
        }

        private sealed class NullPopups : IPopupService, IPopupViews
        {
            public PopupHandle Show(PopupId id) => default;
            public void Close(PopupHandle handle) { }
            public bool TryGetView<T>(PopupHandle handle, out T view) where T : class
            {
                view = null;
                return false;
            }
            public bool IsOnTop(PopupHandle handle) => false;
        }
    }
}
