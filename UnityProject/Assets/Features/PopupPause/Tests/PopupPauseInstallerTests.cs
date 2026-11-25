using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.PopupPause;
using Game.Run;
using Game.Services.Popup;
using NUnit.Framework;
using UnityEditor;
using SessionState = Game.Core.SessionState;

namespace Game.PopupPause.Tests
{
    internal sealed class FakeRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<T>(T service) where T : class => _services[typeof(T)] = service;

        public T Resolve<T>() where T : class
        {
            if (TryResolve(out T service)) return service;
            throw new InvalidOperationException(typeof(T).Name);
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            service = _services.TryGetValue(typeof(T), out var s) ? (T)s : null;
            return service != null;
        }
    }

    internal sealed class FakeSession : ISessionScene
    {
        public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
        public SessionState State => SessionState.Init;
#pragma warning disable CS0067
        public event Action<SessionState> StateChanged;
#pragma warning restore CS0067
        public void Join(ISessionParticipant participant) => Joined.Add(participant);
        public UniTask EndAsync() => UniTask.CompletedTask;
    }

    public class PopupPauseInstallerTests
    {
        private const string InstallerPath = "Assets/Features/PopupPause/FeatureAssets/Public/PopupPauseInstaller.asset";

        private static PopupPauseInstaller LoadInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<PopupPauseInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, $"installer asset missing at {InstallerPath}");
            return installer;
        }

        private static FakeRegistry FullRegistry(FakeSession session, FakeRun run, FakePopups popups)
        {
            var registry = new FakeRegistry();
            registry.Register<ISessionScene>(session);
            registry.Register<IRunFlow>(run);
            registry.Register<IPopupService>(popups);
            registry.Register<IPopupViews>(popups);
            return registry;
        }

        [Test]
        public void RealInstallerAsset_JoinsTheSessionAndOpensThePopupWhenTheRunPauses()
        {
            var session = new FakeSession();
            var run = new FakeRun();
            var popups = new FakePopups();

            LoadInstaller().Install(FullRegistry(session, run, popups));

            Assert.That(session.Joined.Count, Is.EqualTo(1), "the shipped asset has no cheat selected");
            session.Joined[0].Run();
            run.Pause();
            Assert.That(popups.IsOpen, Is.True);
            Assert.That(popups.Shown[0].Value, Is.EqualTo("PopupPause/Main"));
        }

        [TestCase(typeof(ISessionScene), "ISessionScene")]
        [TestCase(typeof(IRunFlow), "IRunFlow")]
        [TestCase(typeof(IPopupService), "IPopupService")]
        [TestCase(typeof(IPopupViews), "IPopupViews")]
        public void MissingDependency_FailsAtOnceNamingIt(Type missing, string name)
        {
            var session = new FakeSession();
            var popups = new FakePopups();
            var registry = new FakeRegistry();
            if (missing != typeof(ISessionScene)) registry.Register<ISessionScene>(session);
            if (missing != typeof(IRunFlow)) registry.Register<IRunFlow>(new FakeRun());
            if (missing != typeof(IPopupService)) registry.Register<IPopupService>(popups);
            if (missing != typeof(IPopupViews)) registry.Register<IPopupViews>(popups);

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(registry));

            StringAssert.Contains(name, error.Message);
            Assert.That(session.Joined, Is.Empty);
        }
    }
}
