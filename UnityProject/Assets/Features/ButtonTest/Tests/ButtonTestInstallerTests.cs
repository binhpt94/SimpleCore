using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SessionState = Game.Core.SessionState;

namespace Game.ButtonTest.Tests
{
    // Asset installer thật nạp được và Install nối đúng một bên tham gia phiên; config đặt canvas dưới lớp popup.
    public sealed class ButtonTestInstallerTests
    {
        private const string InstallerPath = "Assets/Features/ButtonTest/FeatureAssets/Public/ButtonTestInstaller.asset";
        private const string ConfigPath = "Assets/Features/ButtonTest/FeatureAssets/Private/ButtonTestConfig.asset";

        [Test]
        public void InstallerAsset_LoadsAndInstall_JoinsOneParticipant()
        {
            var installer = AssetDatabase.LoadAssetAtPath<ButtonTestInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Không nạp được {InstallerPath}.");

            var session = new FakeSession();
            var registry = new FakeRegistry();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(new TestButtonPresenterTests.FakeRun());

            installer.Install(registry);

            Assert.AreEqual(1, session.Joined.Count);
            Assert.IsInstanceOf<ButtonTestParticipant>(session.Joined[0]);
        }

        [Test]
        public void InstallerAsset_WithoutRun_FailsNamingTheMissingContract()
        {
            var installer = AssetDatabase.LoadAssetAtPath<ButtonTestInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Không nạp được {InstallerPath}.");

            var registry = new FakeRegistry();
            registry.Register<ISessionScene>(new FakeSession());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains(nameof(IRunFlow), error.Message);
        }

        [Test]
        public void ConfigAsset_CanvasDrawsBelowPopupLayer_SoPopupBlocksTaps()
        {
            var config = AssetDatabase.LoadAssetAtPath<ButtonTestConfig>(ConfigPath);
            Assert.IsNotNull(config, $"Không nạp được {ConfigPath}.");

            Assert.Less(config.Layout.SortingOrder, ButtonTestConfig.PopupLayerSortingOrder);
            Assert.DoesNotThrow(config.Validate);
        }

        [Test]
        public void Config_SortingAtOrAbovePopupLayer_IsRejected()
        {
            var config = ScriptableObject.CreateInstance<ButtonTestConfig>();
            try
            {
                typeof(ButtonTestConfig)
                    .GetField("canvasSortingOrder", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(config, ButtonTestConfig.PopupLayerSortingOrder);

                Assert.Throws<InvalidOperationException>(config.Validate);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ViewPrefab_HasTestButtonView()
        {
            var installer = AssetDatabase.LoadAssetAtPath<ButtonTestInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Không nạp được {InstallerPath}.");

            var prefab = (GameObject)typeof(ButtonTestInstaller)
                .GetField("viewPrefab", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(installer);

            Assert.IsNotNull(prefab, "Installer thiếu viewPrefab.");
            Assert.IsNotNull(prefab.GetComponent<ITestButtonView>(), "Prefab thiếu component ITestButtonView.");
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();

            public SessionState State => SessionState.Init;
            public event Action<SessionState> StateChanged { add { } remove { } }
            public void Join(ISessionParticipant participant) => Joined.Add(participant);
            public UniTask EndAsync() => UniTask.CompletedTask;
        }

        private sealed class FakeRegistry : IServiceRegistry
        {
            private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

            public void Register<T>(T service) where T : class => _services[typeof(T)] = service;

            public T Resolve<T>() where T : class =>
                TryResolve<T>(out var s) ? s : throw new InvalidOperationException($"Chưa cài {typeof(T).Name}.");

            public bool TryResolve<T>(out T service) where T : class
            {
                service = _services.TryGetValue(typeof(T), out var s) ? (T)s : null;
                return service != null;
            }
        }
    }
}
