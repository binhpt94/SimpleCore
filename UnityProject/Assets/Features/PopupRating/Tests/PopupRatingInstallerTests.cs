using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;
using Game.Services.Popup;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SessionState = Game.Core.SessionState;

namespace Game.PopupRating.Tests
{
    public sealed class PopupRatingInstallerTests
    {
        private const string InstallerPath = "Assets/Features/PopupRating/FeatureAssets/Public/PopupRatingInstaller.asset";
        private const string ConfigPath = "Assets/Features/PopupRating/FeatureAssets/Private/PopupRatingConfig.asset";
        private const string PrefabPath = "Assets/Features/PopupRating/FeatureAssets/Public/PopupRatingMain.prefab";

        private static PopupRatingInstaller LoadInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<PopupRatingInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"installer asset missing at {InstallerPath}");
            return installer;
        }

        private static ServiceRegistry FullRegistry(FakeSession session)
        {
            var registry = new ServiceRegistry();
            var popups = new RatingFakes.FakePopups();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(new RatingFakes.FakeFlow());
            registry.Register<IRunScore>(new RatingFakes.FakeScore());
            registry.Register<IPopupService>(popups);
            registry.Register<IPopupViews>(popups);
            return registry;
        }

        [Test]
        public void RealInstallerJoinsTheSessionWithoutCheat()
        {
            var installer = LoadInstaller();
            Assert.That(installer.Cheat, Is.EqualTo(RatingCheatState.None), "the shipped asset keeps the cheat off");
            var session = new FakeSession();
            installer.Install(FullRegistry(session));
            Assert.That(session.Joined, Has.Count.EqualTo(1));
            Assert.That(session.Joined[0], Is.InstanceOf<RatingParticipant>());
        }

        [Test]
        public void RealConfigHoldsTheIdeaValuesOfInviteParamsAndTimingParams()
        {
            var config = AssetDatabase.LoadAssetAtPath<RatingConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, $"config missing at {ConfigPath}");
            Assert.That(LoadInstaller().Config, Is.SameAs(config));
            Assert.That(config.InviteText, Is.EqualTo("Đánh giá game trên Google Play"), "invite_text");
            Assert.That(config.RateLabel, Is.EqualTo("Đánh giá"), "rate_label");
            Assert.That(config.LaterLabel, Is.EqualTo("Để sau"), "later_label");
            Assert.That(config.StoreOpen, Is.EqualTo(StoreOpenOrder.AppThenBrowser), "store_open_order");
            Assert.That(config.InviteDelay, Is.EqualTo(1.5f), "invite_delay");
            Assert.That(config.InviteDelay, Is.GreaterThan(1f), "after the 1 s lose popup delay of feature-run");
            Assert.That(config.MaxInvitesPerLaunch, Is.EqualTo(1), "max_invites_per_launch");
        }

        [Test]
        public void MissingRunIsNamedAtInstall()
        {
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(registry));
            Assert.That(error.Message, Does.Contain(nameof(IRunFlow)));
        }

        [Test]
        public void MissingConfigIsNamedAtInstall()
        {
            var installer = ScriptableObject.CreateInstance<PopupRatingInstaller>();
            try
            {
                var error = Assert.Throws<InvalidOperationException>(() => installer.Install(FullRegistry(new FakeSession())));
                Assert.That(error.Message, Does.Contain(nameof(RatingConfig)));
            }
            finally { UnityEngine.Object.DestroyImmediate(installer); }
        }

        [Test]
        public void CheatInviteOpenAlsoJoinsTheCheat()
        {
            var installer = ScriptableObject.CreateInstance<PopupRatingInstaller>();
            var config = ScriptableObject.CreateInstance<RatingConfig>();
            try
            {
                installer.Config = config;
                installer.Cheat = RatingCheatState.InviteOpen;
                var session = new FakeSession();
                installer.Install(FullRegistry(session));
                Assert.That(session.Joined, Has.Count.EqualTo(2));
                Assert.That(session.Joined[1], Is.InstanceOf<RatingCheatParticipant>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PopupPrefabIsUiWithRatingViewAndTwoButtonsRateFirst()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, $"popup prefab missing at {PrefabPath}");
            Assert.That(prefab.transform, Is.InstanceOf<RectTransform>());
            var view = prefab.GetComponent<IRatingView>() as Component;
            Assert.That(view, Is.Not.Null, "IRatingView on the root");
            var so = new SerializedObject(view);
            var rate = (Component)so.FindProperty("_rate").objectReferenceValue;
            var later = (Component)so.FindProperty("_later").objectReferenceValue;
            Assert.That(rate, Is.Not.Null);
            Assert.That(later, Is.Not.Null);
            Assert.That(so.FindProperty("_inviteText").objectReferenceValue, Is.Not.Null);
            var rateRect = (RectTransform)rate.transform;
            var laterRect = (RectTransform)later.transform;
            Assert.That(rateRect.anchoredPosition.y, Is.GreaterThan(laterRect.anchoredPosition.y), "main button comes first");
            Assert.That(rateRect.sizeDelta.x, Is.GreaterThan(laterRect.sizeDelta.x), "main button stands out");
        }

        [Test]
        public void BackdropTouchDoesNotCloseTheInvite_ThePrefabHasOnlyRateAndLaterAsTouchTargets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, $"popup prefab missing at {PrefabPath}");
            var view = prefab.GetComponent<IRatingView>() as Component;
            var so = new SerializedObject(view);
            var rate = so.FindProperty("_rate").objectReferenceValue;
            var later = so.FindProperty("_later").objectReferenceValue;

            // No UnityEngine.UI reference in this test assembly: find touch targets by type name.
            var all = prefab.GetComponentsInChildren<Component>(true);
            var selectables = System.Array.FindAll(all, c => IsKindOf(c, "Selectable"));
            Assert.That(selectables, Has.Length.EqualTo(2), "only the two buttons take touches");
            Assert.That(selectables, Is.EquivalentTo(new UnityEngine.Object[] { rate, later }));
            var clickables = System.Array.FindAll(all, c => c is UnityEngine.EventSystems.IPointerClickHandler);
            Assert.That(clickables, Has.Length.EqualTo(2), "no other tap area on the panel or around it");
            Assert.That(System.Array.Exists(prefab.GetComponents<Component>(), c => IsKindOf(c, "Selectable")),
                Is.False, "the panel itself is not a button");
        }

        private static bool IsKindOf(Component c, string typeName)
        {
            for (var t = c.GetType(); t != null; t = t.BaseType)
                if (t.Name == typeName) return true;
            return false;
        }

        internal sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
            public SessionState State => SessionState.Init;
            public event Action<SessionState> StateChanged { add { } remove { } }
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public UniTask EndAsync() => UniTask.CompletedTask;
        }
    }
}
