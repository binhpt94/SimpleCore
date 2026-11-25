using System;
using Game.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Ads
{
    /// <summary>
    /// Ads inside one scene session: creates the view (frame beat and, in dev builds, the fake ad screen) when the
    /// session runs; when it ends, the running request is dropped without a result and the view is destroyed.
    /// For the session's clean-up check, the view is tracked while it lives and a waiting request counts as a job.
    /// </summary>
    internal sealed class AdsSessionParticipant : SessionParticipantBase
    {
        private readonly AdRequests _requests;
        private readonly GameObject _viewPrefab;
        private IDisposable _waitJob;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly FakeAdProvider _fake;

        public AdsSessionParticipant(AdRequests requests, GameObject viewPrefab, FakeAdProvider fake)
            : this(requests, viewPrefab)
        {
            _fake = fake;
        }
#endif

        public AdsSessionParticipant(AdRequests requests, GameObject viewPrefab)
        {
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _viewPrefab = viewPrefab ? viewPrefab : throw new ArgumentNullException(nameof(viewPrefab));
        }

        protected override bool OpensViewsOnRun => true;

        protected override void OnRun()
        {
            var view = Object.Instantiate(_viewPrefab);
            var heldView = TrackView(view);
            WhenDisposed(() =>
            {
                heldView.Dispose();
                if (Application.isPlaying) Object.Destroy(view);
                else Object.DestroyImmediate(view);
            });

            var frames = view.GetComponent<IAdsFrames>()
                         ?? throw new InvalidOperationException(
                             $"Ads: view prefab '{_viewPrefab.name}' has no IAdsFrames component.");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_fake != null)
            {
                var screen = view.GetComponent<IFakeAdScreen>()
                             ?? throw new InvalidOperationException(
                                 $"Ads: view prefab '{_viewPrefab.name}' has no IFakeAdScreen component.");
                screen.Hide();
                _fake.AttachScreen(screen);
                WhenDisposed(() => _fake.AttachScreen(null));
            }
#endif

            frames.Frame += OnFrame;
            WhenDisposed(() => frames.Frame -= OnFrame);

            _requests.BusyChanged += SyncWaitJob;
            SyncWaitJob();
            WhenDisposed(() =>
            {
                _requests.BusyChanged -= SyncWaitJob;
                _waitJob?.Dispose();
                _waitJob = null;
            });

            // Registered last so it runs first: the request is dropped before the view goes away.
            WhenDisposed(_requests.Shutdown);
        }

        /// <summary>A request waiting for its result (open, play, confirm, game back on screen) is one running job.</summary>
        private void SyncWaitJob()
        {
            if (_requests.IsBusy && _waitJob == null) _waitJob = TrackJob();
            else if (!_requests.IsBusy && _waitJob != null)
            {
                _waitJob.Dispose();
                _waitJob = null;
            }
        }

        private void OnFrame(float deltaSeconds, bool gameOnScreen)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _fake?.Tick(deltaSeconds);
#endif
            _requests.Tick(deltaSeconds, gameOnScreen);
        }
    }
}
