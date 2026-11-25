using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Ads.Tests
{
    /// <summary>
    /// The Ads view and a waiting ad request are reported to the session's clean-up check; after the session ends
    /// both counts are back to 0. Counting runs in the Editor, where EditMode tests run.
    /// </summary>
    public class AdsSessionUsageTests
    {
        private const string ViewPrefabPath = "Assets/Features/Ads/FeatureAssets/Private/AdsView.prefab";

        private GameObject _prefab;
        private FakeAdProvider _fake;
        private AdRequests _requests;
        private AdsSessionParticipant _participant;
        private ISessionParticipantUsage Usage => _participant;

        [SetUp]
        public void SetUp()
        {
            _prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefabPath);
            Assert.IsNotNull(_prefab, $"view prefab missing at {ViewPrefabPath}");
            _fake = new FakeAdProvider(FakeAdOutcome.Watched, 2f);
            _requests = new AdRequests(_fake, new QuietLog(), 8f, 5f);
            _participant = new AdsSessionParticipant(_requests, _prefab, _fake);
        }

        [TearDown]
        public void TearDown()
        {
            // Disposing destroys the view clone (DestroyImmediate outside Play).
            _participant?.DisposeAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void TheAdsViewOpensOnRunAndIsCounted()
        {
            Assert.IsTrue(Usage.OpensViewsOnRun);

            _participant.Run();

            Assert.AreEqual(1, Usage.HeldViews);
            Assert.AreEqual(0, Usage.RunningJobs, "no ad is waiting yet");
        }

        [Test]
        public void AWaitingAdRequestCountsAsOneJobUntilItsResultIsGiven()
        {
            _participant.Run();
            var replies = new List<AdReply>();

            _requests.Show(1, replies.Add);
            Assert.AreEqual(1, Usage.RunningJobs);

            _requests.Show(2, replies.Add);
            Assert.AreEqual(1, Usage.RunningJobs, "a busy reply is not a job");

            for (var i = 0; i < 25; i++)
            {
                _fake.Tick(0.1f);
                _requests.Tick(0.1f, true);
            }

            Assert.AreEqual(AdResult.Watched, replies[1].Result);
            Assert.AreEqual(0, Usage.RunningJobs);
        }

        [Test]
        public void CreatedThenDisposed_HoldsNoViewAndNoJob()
        {
            _participant.Run();
            _requests.Show(1, _ => { });
            Assert.AreEqual(1, Usage.HeldViews);
            Assert.AreEqual(1, Usage.RunningJobs);

            _participant.DisposeAsync().GetAwaiter().GetResult();

            Assert.AreEqual(0, Usage.HeldViews);
            Assert.AreEqual(0, Usage.RunningJobs);
            Assert.IsFalse(_requests.IsBusy);
        }

        [Test]
        public void HeldCheatAdCountsAsAJobUntilCancelled()
        {
            _participant.Run();
            ((IAdsCheat)_fake).HoldNextAd();
            _requests.Show(1, _ => { });
            Assert.AreEqual(1, Usage.RunningJobs);

            _requests.Cancel(1);

            Assert.AreEqual(0, Usage.RunningJobs);
        }

        private sealed class QuietLog : IAdsLog
        {
            public void NotWatched(int offerId, AdFailReason reason)
            {
            }

            public void Dropped(int offerId, AdResult result)
            {
            }
        }
    }
}
