using System;
using Game.Core;
using Game.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Home.Tests
{
    /// <summary>The real Home assets: numbers of the design, and Install in the Game scene.</summary>
    public sealed class HomeInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Home/FeatureAssets/Public/HomeInstaller.asset";
        private const string ConfigPath = "Assets/Features/Home/FeatureAssets/Private/HomeConfig.asset";

        private static HomeInstaller LoadInstaller()
        {
            var installer = AssetDatabase.LoadAssetAtPath<HomeInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"Missing installer asset at {InstallerPath}");
            return installer;
        }

        private static ServiceRegistry FullRegistry(FakeSessionScene session)
        {
            var registry = new ServiceRegistry();
            registry.Register<ICheatSelection>(new HomeInstallerCheatTests.FakeCheatSelection());
            registry.Register<IRunScore>(new FakeRunScore());
            registry.Register<ISessionScene>(session);
            return registry;
        }

        [Test]
        public void ConfigAssetHoldsTheDesignNumbers()
        {
            var config = AssetDatabase.LoadAssetAtPath<HomeConfig>(ConfigPath);
            Assert.IsNotNull(config, $"Missing config asset at {ConfigPath}");
            config.Check();

            Assert.AreEqual("Thủ thành anh hùng", config.Title);
            Assert.AreEqual("Kỉ lục: 5 đợt", HomeScreenPresenter.RecordText(config, 5));
            Assert.AreEqual("Chơi", config.PlayLabel);
            Assert.AreEqual(0, config.RecordWhenNone);
            Assert.AreEqual(1, config.TapsPerShow);
            Assert.AreEqual(new Vector2(1920, 1080), config.ReferenceScreen);
            Assert.AreEqual(new Rect(360, 200, 1200, 160), config.TitleRect);
            Assert.AreEqual(new Rect(660, 420, 600, 80), config.RecordRect);
            Assert.AreEqual(new Rect(760, 620, 400, 120), config.PlayRect);

            var layout = HomeLayout.From(config);
            Assert.AreEqual(3, layout.Elements.Count);
        }

        [Test]
        public void InstallJoinsTheGameSceneSession()
        {
            var session = new FakeSessionScene();

            LoadInstaller().Install(FullRegistry(session));

            Assert.AreEqual(1, session.Joined.Count);
            Assert.IsInstanceOf<HomeParticipant>(session.Joined[0]);
        }

        [Test]
        public void InstallRegistersTheStartScreenForTheScreenFlow()
        {
            var registry = FullRegistry(new FakeSessionScene());

            LoadInstaller().Install(registry);

            Assert.IsTrue(registry.TryResolve<IHomeScreen>(out var screen));
            Assert.IsNotNull(screen);
        }

        [Test]
        public void InstallWithoutScoreFailsNamingIt()
        {
            var registry = new ServiceRegistry();
            registry.Register<ICheatSelection>(new HomeInstallerCheatTests.FakeCheatSelection());
            registry.Register<ISessionScene>(new FakeSessionScene());

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(registry));
            StringAssert.Contains(nameof(IRunScore), error.Message);
        }

        [Test]
        public void InstallWithoutSessionFailsNamingIt()
        {
            var registry = new ServiceRegistry();
            registry.Register<ICheatSelection>(new HomeInstallerCheatTests.FakeCheatSelection());
            registry.Register<IRunScore>(new FakeRunScore());

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(registry));
            StringAssert.Contains(nameof(ISessionScene), error.Message);
        }

        [Test]
        public void StartScreenViewIsHeldFromTheRunStepAndReleasedToZeroWhenSessionEnds()
        {
            var config = ScriptableObject.CreateInstance<HomeConfig>();
            try
            {
                var factory = new FakeHomeFactory();
                var participant = new HomeParticipant(new HomeScreenPresenter(new FakeRunScore(), config), config, factory);
                var usage = (ISessionParticipantUsage)participant;

                Assert.IsTrue(usage.OpensViewsOnRun, "The game opens on the start screen, so Home builds its view in the run step.");
                Assert.AreEqual(0, usage.HeldViews);

                participant.Run();
                Assert.AreEqual(1, usage.HeldViews);

                participant.DisposeAsync().GetAwaiter().GetResult();
                Assert.AreEqual(0, usage.HeldViews);
                Assert.IsTrue(factory.Display.Disposed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ScreenIsBuiltHiddenWhenSessionRunsAndRemovedWhenItEnds()
        {
            var config = ScriptableObject.CreateInstance<HomeConfig>();
            try
            {
                var factory = new FakeHomeFactory();
                var screen = new HomeScreenPresenter(new FakeRunScore(), config);
                var participant = new HomeParticipant(screen, config, factory);

                participant.Run();
                Assert.IsNotNull(factory.Layout);
                Assert.IsFalse(factory.Display.Visible, "The start screen waits for the screen flow to show it.");

                screen.Show();
                Assert.IsTrue(factory.Display.Visible);

                participant.DisposeAsync().GetAwaiter().GetResult();
                Assert.IsTrue(factory.Display.Disposed);
                Assert.IsFalse(screen.IsShown);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
