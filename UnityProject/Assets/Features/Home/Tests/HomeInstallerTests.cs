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
            registry.Register<IRunFlow>(new FakeRunFlow());
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
        public void InstallWithoutRunFailsNamingIt()
        {
            var registry = new ServiceRegistry();
            registry.Register<IRunScore>(new FakeRunScore());
            registry.Register<ISessionScene>(new FakeSessionScene());

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(registry));
            StringAssert.Contains(nameof(IRunFlow), error.Message);
        }

        [Test]
        public void InstallWithoutSessionFailsNamingIt()
        {
            var registry = new ServiceRegistry();
            registry.Register<IRunFlow>(new FakeRunFlow());
            registry.Register<IRunScore>(new FakeRunScore());

            var error = Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(registry));
            StringAssert.Contains(nameof(ISessionScene), error.Message);
        }

        [Test]
        public void ScreenIsBuiltWhenSessionRunsAndRemovedWhenItEnds()
        {
            var config = ScriptableObject.CreateInstance<HomeConfig>();
            try
            {
                var factory = new FakeHomeFactory();
                var participant = new HomeParticipant(new FakeRunFlow(), new FakeRunScore(), config, factory);

                participant.Run();
                Assert.IsNotNull(factory.Layout);
                Assert.IsTrue(factory.Display.Visible);

                participant.DisposeAsync().GetAwaiter().GetResult();
                Assert.IsTrue(factory.Display.Disposed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
