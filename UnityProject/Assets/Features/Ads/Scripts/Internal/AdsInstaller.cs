using System;
using Game.Core;
using UnityEngine;

namespace Game.Ads
{
    /// <summary>
    /// Wires the Ads feature: publishes <see cref="IRewardedAds"/> and joins the scene session. Dev builds use the
    /// fake provider (outcome chosen in AdsConfig) and also publish <see cref="IAdsCheat"/>; release builds have no ad
    /// network yet and always answer NotWatched.
    /// Install after SessionScene.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Ads/Ads Installer", fileName = "AdsInstaller")]
    public sealed class AdsInstaller : InstallerAsset
    {
        [SerializeField] private AdsConfig _config;

        [Tooltip("The Ads view: frame beat (AdsFrameView) and the dev fake ad screen (FakeAdScreenView).")]
        [SerializeField] private GameObject _viewPrefab;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (_config == null) throw new InvalidOperationException("AdsInstaller: AdsConfig is missing.");
            if (_viewPrefab == null) throw new InvalidOperationException("AdsInstaller: the Ads view prefab is missing.");
            var session = registry.Resolve<ISessionScene>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var fake = new FakeAdProvider(_config.FakeOutcome, _config.FakeScreenSeconds);
            var requests = new AdRequests(fake, new UnityAdsLog(), _config.OpenLimitSeconds, _config.ConfirmLimitSeconds);
            registry.Register<IRewardedAds>(requests);
            registry.Register<IAdsCheat>(fake);
            session.Join(new AdsSessionParticipant(requests, _viewPrefab, fake));
#else
            var requests = new AdRequests(new NoAdProvider(), new UnityAdsLog(),
                _config.OpenLimitSeconds, _config.ConfirmLimitSeconds);
            registry.Register<IRewardedAds>(requests);
            session.Join(new AdsSessionParticipant(requests, _viewPrefab));
#endif
        }
    }
}
