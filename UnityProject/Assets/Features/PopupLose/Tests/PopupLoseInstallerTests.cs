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

namespace Game.PopupLose.Tests
{
    public sealed class PopupLoseInstallerTests
    {
        private const string InstallerPath = "Assets/Features/PopupLose/FeatureAssets/Public/PopupLoseInstaller.asset";
        private const string PrefabPath = "Assets/Features/PopupLose/FeatureAssets/Public/LoseMain.prefab";

        private static PopupLoseInstaller LoadInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<PopupLoseInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"installer asset missing at {InstallerPath}");
            return installer;
        }

        private static ServiceRegistry FullRegistry(FakeSession session, LosePopupTests.FakeFlow flow)
        {
            var registry = new ServiceRegistry();
            var popups = new LosePopupTests.FakePopups();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(flow);
            registry.Register<IRunScore>(new LosePopupTests.FakeScore());
            registry.Register<IPopupService>(popups);
            registry.Register<IPopupViews>(popups);
            return registry;
        }

        [Test]
        public void RealInstallerJoinsTheSessionWithoutCheat()
        {
            var installer = LoadInstaller();
            Assert.That(installer.Cheat, Is.EqualTo(LoseCheatState.None), "the shipped asset keeps the cheat off");
            Assert.That(installer.HomeButton, Is.True, "PROTOTYPE: Về màn bắt đầu is on (actions-spec, Thông số theo giai đoạn)");
            var session = new FakeSession();

            installer.Install(FullRegistry(session, new LosePopupTests.FakeFlow()));

            Assert.That(session.Joined, Has.Count.EqualTo(1));
            Assert.That(session.Joined[0], Is.InstanceOf<LosePopupParticipant>());
        }

        [Test]
        public void MissingRunIsNamedAtInstall()
        {
            var installer = LoadInstaller();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            Assert.That(error.Message, Does.Contain(nameof(IRunFlow)));
        }

        [Test]
        public void MissingPopupServiceIsNamedAtInstall()
        {
            var installer = LoadInstaller();
            var registry = new ServiceRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            registry.Register<IRunFlow>(new LosePopupTests.FakeFlow());
            registry.Register<IRunScore>(new LosePopupTests.FakeScore());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            Assert.That(error.Message, Does.Contain(nameof(IPopupService)));
        }

        [Test]
        public void CheatStateAlsoJoinsTheCheat()
        {
            var installer = ScriptableObject.CreateInstance<PopupLoseInstaller>();
            try
            {
                installer.Cheat = LoseCheatState.NewRecord;
                var session = new FakeSession();

                installer.Install(FullRegistry(session, new LosePopupTests.FakeFlow()));

                Assert.That(session.Joined, Has.Count.EqualTo(2));
                Assert.That(session.Joined[1], Is.InstanceOf<LoseCheatParticipant>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(installer);
            }
        }

        [Test]
        public void PopupPrefabIsUiWithLoseViewOnItsRoot()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, $"popup prefab missing at {PrefabPath}");
            Assert.That(prefab.transform, Is.InstanceOf<RectTransform>());
            Assert.That(prefab.GetComponent<ILoseView>(), Is.Not.Null);
        }

        [Test]
        public void PopupPrefabHasReplayAsTheMainButtonAndHomeAsTheSecond()
        {
            var view = LoadView();
            var replay = ReferencedRect(view, "_replay");
            var home = ReferencedRect(view, "_home");

            Assert.That(LabelText(replay), Is.EqualTo("Chơi lại"));
            Assert.That(LabelText(home), Is.EqualTo("Về màn bắt đầu"));
            Assert.That(replay.anchoredPosition.x, Is.LessThan(home.anchoredPosition.x), "main button comes first");
            Assert.That(replay.sizeDelta.x, Is.GreaterThan(home.sizeDelta.x), "main button stands out");
            Assert.That(replay.sizeDelta, Is.EqualTo(new Vector2(400, 120)));
            Assert.That(home.sizeDelta, Is.EqualTo(new Vector2(260, 120)));
            Assert.That(CountComponents(view.gameObject, "Button"), Is.EqualTo(2), "exactly two buttons");
        }

        [Test]
        public void NewRecordLineSitsOnItsOwnBandThatPulsesWithIt()
        {
            var view = LoadView();
            var band = ReferencedRect(view, "_newRecord");

            Assert.That(band.sizeDelta, Is.EqualTo(new Vector2(400, 70)));
            Assert.That(CountComponents(band.gameObject, "Image"), Is.GreaterThanOrEqualTo(1), "band has its own image");
            Assert.That(LabelText(band), Is.EqualTo("Kỉ lục mới!"));
        }

        private static Component LoadView()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, $"popup prefab missing at {PrefabPath}");
            var view = prefab.GetComponent<ILoseView>() as Component;
            Assert.That(view, Is.Not.Null);
            return view;
        }

        private static RectTransform ReferencedRect(Component view, string field)
        {
            var property = new SerializedObject(view).FindProperty(field);
            Assert.That(property, Is.Not.Null, $"view has no field {field}");
            var value = property.objectReferenceValue;
            var target = value is GameObject go ? go.transform : (value as Component)?.transform;
            Assert.That(target, Is.Not.Null, $"{field} is not wired in the prefab");
            return (RectTransform)target;
        }

        private static string LabelText(RectTransform root)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.GetType().Name != "TextMeshProUGUI") continue;
                return new SerializedObject(component).FindProperty("m_text").stringValue;
            }
            Assert.Fail($"{root.name} has no text");
            return null;
        }

        private static int CountComponents(GameObject root, string typeName)
        {
            var count = 0;
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component != null && component.GetType().Name == typeName) count++;
            return count;
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
