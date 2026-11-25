using System;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.GameFlow.Tests
{
    public class GameFlowInstallerTests
    {
        private const string InstallerPath = "Assets/Features/GameFlow/FeatureAssets/Public/GameFlowInstaller.asset";
        private const string ConfigPath = "Assets/Features/GameFlow/FeatureAssets/Private/GameFlowConfig.asset";

        private readonly List<UnityEngine.Object> _made = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _made) UnityEngine.Object.DestroyImmediate(o);
            _made.Clear();
        }

        [Test]
        public void InstallerAsset_InstallsWithItsRealConfig_WithoutRegisteringOrJoiningAnything()
        {
            var installer = AssetDatabase.LoadAssetAtPath<GameFlowInstaller>(InstallerPath);
            Assert.That(installer, Is.Not.Null, InstallerPath + " missing");
            var registry = new FakeRegistry();

            Assert.DoesNotThrow(() => installer.Install(registry));
            Assert.That(registry.Registered, Is.Empty);
            Assert.That(registry.Resolved, Is.Empty);
        }

        [Test]
        public void ConfigAsset_HoldsTheDesignedValues_AndLeavesScreenSwitchingToTheRunAtIdea()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameFlowConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, ConfigPath + " missing");

            var settings = config.ToSettings();
            Assert.That(config.RouteScreens, Is.False);
            Assert.That(settings.SwitchSeconds, Is.EqualTo(0f));
            Assert.That(settings.BuildLimitSeconds, Is.EqualTo(3f));
            Assert.That(settings.BuildFailedText, Is.EqualTo("Không vào được trận, thử lại"));
            Assert.That(settings.BackButtonMinSidePx, Is.EqualTo(120));
            Assert.That(settings.BackButtonCorner, Is.EqualTo(ScreenCorner.TopLeft));
        }

        [Test]
        public void Install_WithoutConfig_FailsNamingTheConfig()
        {
            var installer = Make<GameFlowInstaller>();

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(new FakeRegistry()));
            Assert.That(error.Message, Does.Contain("GameFlowConfig"));
        }

        [Test]
        public void Install_WithAConfigValueThatCannotWork_Fails()
        {
            var config = Make<GameFlowConfig>();
            config.SetBuildLimitForTest(0f);
            var installer = Make<GameFlowInstaller>();
            installer.SetConfigForTest(config);

            Assert.Throws<ArgumentOutOfRangeException>(() => installer.Install(new FakeRegistry()));
        }

        [Test]
        public void Install_WithRouteScreensOn_FailsBecauseTheRunStillSwitchesScreens()
        {
            var config = Make<GameFlowConfig>();
            config.SetRouteScreensForTest(true);
            var installer = Make<GameFlowInstaller>();
            installer.SetConfigForTest(config);

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(new FakeRegistry()));
            Assert.That(error.Message, Does.Contain("Route screens"));
        }

        private T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _made.Add(o);
            return o;
        }

        private sealed class FakeRegistry : IServiceRegistry
        {
            public readonly List<Type> Registered = new List<Type>();
            public readonly List<Type> Resolved = new List<Type>();

            public void Register<T>(T service) where T : class => Registered.Add(typeof(T));

            public T Resolve<T>() where T : class
            {
                Resolved.Add(typeof(T));
                throw new InvalidOperationException(typeof(T).Name + " not installed");
            }

            public bool TryResolve<T>(out T service) where T : class
            {
                Resolved.Add(typeof(T));
                service = null;
                return false;
            }
        }
    }
}
