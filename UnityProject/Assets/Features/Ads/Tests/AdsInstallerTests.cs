using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Ads.Tests
{
    /// <summary>The real installer asset wires the feature in EditMode.</summary>
    public class AdsInstallerTests
    {
        private const string InstallerPath = "Assets/Features/Ads/FeatureAssets/Public/AdsInstaller.asset";

        private static AdsInstaller LoadInstaller()
        {
            var installer = UnityEditor.AssetDatabase.LoadAssetAtPath<AdsInstaller>(InstallerPath);
            Assert.IsNotNull(installer, $"installer asset missing at {InstallerPath}");
            return installer;
        }

        [Test]
        public void InstallerAssetPublishesRewardedAdsAndJoinsTheSession()
        {
            var registry = new FakeRegistry();
            var session = new FakeSession();
            registry.Register<ISessionScene>(session);

            LoadInstaller().Install(registry);

            Assert.IsTrue(registry.TryResolve<IRewardedAds>(out var ads));
            Assert.IsNotNull(ads);
            Assert.AreEqual(1, session.Joined.Count);
        }

        [Test]
        public void InstalledAdsAnswersBusyRequestsAtOnce()
        {
            var registry = new FakeRegistry();
            registry.Register<ISessionScene>(new FakeSession());
            LoadInstaller().Install(registry);
            var ads = registry.Resolve<IRewardedAds>();

            var replies = new List<AdReply>();
            ads.Show(1, replies.Add);
            ads.Show(2, replies.Add);

            Assert.AreEqual(1, replies.Count);
            Assert.AreEqual(2, replies[0].OfferId);
            Assert.AreEqual(AdResult.NotWatched, replies[0].Result);
        }

        [Test]
        public void InstallWithoutSessionSceneFails()
        {
            Assert.Throws<InvalidOperationException>(() => LoadInstaller().Install(new FakeRegistry()));
        }

        private sealed class FakeRegistry : IServiceRegistry
        {
            private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

            public void Register<T>(T service) where T : class => _services.Add(typeof(T), service);

            public T Resolve<T>() where T : class =>
                TryResolve<T>(out var service)
                    ? service
                    : throw new InvalidOperationException($"{typeof(T).Name} is not installed");

            public bool TryResolve<T>(out T service) where T : class
            {
                var found = _services.TryGetValue(typeof(T), out var value);
                service = value as T;
                return found;
            }
        }

        private sealed class FakeSession : ISessionScene
        {
            public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();

            public Game.Core.SessionState State => Game.Core.SessionState.Init;

            public event Action<Game.Core.SessionState> StateChanged
            {
                add { }
                remove { }
            }

            public void Join(ISessionParticipant participant) => Joined.Add(participant);

            public UniTask EndAsync() => UniTask.CompletedTask;
        }
    }
}
