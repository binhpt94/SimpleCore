using UnityEngine;

namespace Game.Ads
{
    /// <summary>What the dev fake provider does with the next ads; chosen before Play to try the revive flow.</summary>
    internal enum FakeAdOutcome
    {
        /// <summary>The wait screen shows, then the reward is confirmed and the ad closes.</summary>
        Watched,

        /// <summary>No ad: the provider reports no fill at once.</summary>
        NotWatched,

        /// <summary>The wait screen shows and the ad closes, but no confirmation ever comes.</summary>
        TimedOut,
    }

    /// <summary>Ad request numbers. The asset lives in FeatureAssets/Private and is read-only while playing.</summary>
    [CreateAssetMenu(menuName = "Game/Ads/Ads Config", fileName = "AdsConfig")]
    internal sealed class AdsConfig : ScriptableObject
    {
        [Tooltip("Seconds to wait for the ad to start playing; then NotWatched.")]
        [SerializeField, Min(0.1f)] private float _openLimitSeconds = 8f;

        [Tooltip("Seconds to wait for the reward confirmation after the ad closed; then NotWatched.")]
        [SerializeField, Min(0.1f)] private float _confirmLimitSeconds = 5f;

        [Tooltip("Dev builds only: how long the fake provider's wait screen stays up instead of an ad.")]
        [SerializeField, Min(0f)] private float _fakeScreenSeconds = 2f;

        [Tooltip("Dev builds only: what the fake provider does.")]
        [SerializeField] private FakeAdOutcome _fakeOutcome = FakeAdOutcome.Watched;

        public float OpenLimitSeconds => _openLimitSeconds;
        public float ConfirmLimitSeconds => _confirmLimitSeconds;
        public float FakeScreenSeconds => _fakeScreenSeconds;
        public FakeAdOutcome FakeOutcome => _fakeOutcome;

        /// <summary>For tests: an in-memory config with the given numbers.</summary>
        internal static AdsConfig CreateForTests(float openLimit, float confirmLimit, float fakeScreen, FakeAdOutcome outcome)
        {
            var config = CreateInstance<AdsConfig>();
            config._openLimitSeconds = openLimit;
            config._confirmLimitSeconds = confirmLimit;
            config._fakeScreenSeconds = fakeScreen;
            config._fakeOutcome = outcome;
            return config;
        }
    }
}
