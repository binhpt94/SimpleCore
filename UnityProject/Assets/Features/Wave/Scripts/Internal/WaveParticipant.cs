using System;
using Game.Core;
using Game.SimClock;

namespace Game.Wave
{
    /// <summary>
    /// Wave's part of the scene session: listens to the run and the enemies, and steps the waves on the shared clock.
    /// For the session's clean-up check it reports its jobs: the step subscription for as long as it lives, and the
    /// send or break clock while a wave is being sent or the break runs. Wave has no view.
    /// </summary>
    internal sealed class WaveParticipant : SessionParticipantBase
    {
        /// <summary>Wave's name in the shared clock's step order and in the random stream list.</summary>
        internal const string Owner = "Wave";

        private readonly WaveDirector _director;
        private readonly ISimClock _clock;

        private IDisposable _timerJob;

        public WaveParticipant(WaveDirector director, ISimClock clock)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        protected override void OnRun()
        {
            _director.Attach();
            WhenDisposed(_director.Detach);

            var stepJob = TrackJob();
            var steps = _clock.OnStep(Owner, _director.Step);
            WhenDisposed(() =>
            {
                steps.Dispose();
                stepJob.Dispose();
            });

            _director.RunningChanged += OnRunningChanged;
            OnRunningChanged(_director.IsRunning);
            WhenDisposed(() =>
            {
                _director.RunningChanged -= OnRunningChanged;
                OnRunningChanged(false);
            });
        }

        private void OnRunningChanged(bool running)
        {
            if (running && _timerJob == null) _timerJob = TrackJob();
            else if (!running && _timerJob != null)
            {
                _timerJob.Dispose();
                _timerJob = null;
            }
        }
    }
}
