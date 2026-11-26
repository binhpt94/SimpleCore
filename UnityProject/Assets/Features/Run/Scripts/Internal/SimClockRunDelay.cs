using System;
using System.Collections.Generic;
using Game.SimClock;

namespace Game.Run
{
    /// <summary>
    /// The run's gameplay waits on the shared clock, at the run's place in each step (owner "Run", last in the order).
    /// <see cref="Schedule"/> counts the length of every step after the one it was asked in, paused or not: the run's
    /// own waits (before the lose popup, before the revive popup) happen while the battle is stopped.
    /// <see cref="NextFrame"/> runs at the run's next place in a step, so a wave reported cleared earlier in a step
    /// counts at the end of that step unless the castle fell in it.
    /// It listens to the clock from <see cref="Listen"/> (the scene session runs) or the first wait; dispose it when the session ends.
    /// </summary>
    internal sealed class SimClockRunDelay : IRunDelay, IRunWaitHold, IDisposable
    {
        // While the game is off the screen, steps do not count toward timed waits (flow-rules: time away does not count).
        private bool _held;

        // Step lengths are floats; a sum short by rounding still ends the wait.
        private const double Tolerance = 1e-6;

        private readonly ISimClock _clock;
        private readonly List<Pending> _pending = new List<Pending>();
        private IDisposable _listening;
        private IDisposable _listeningJob;
        private Func<IDisposable> _trackJob;
        private bool _disposed;

        public SimClockRunDelay(ISimClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public IDisposable Schedule(float seconds, Action then)
        {
            if (then == null) throw new ArgumentNullException(nameof(then));
            if (seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
            return Add(new Pending(this, then, seconds, _clock.StepCount));
        }

        public IDisposable NextFrame(Action then)
        {
            if (then == null) throw new ArgumentNullException(nameof(then));
            return Add(new Pending(this, then, null, 0));
        }

        /// <summary>
        /// Starts listening at the run's place in the step order (the scene session runs). Done here rather than on
        /// the first wait so the run is never added to the clock in the middle of a step, and a step order without
        /// "Run" fails at once.
        /// </summary>
        /// <param name="trackJob">
        /// Counts a running job for the session's clean-up check (SessionParticipantBase.TrackJob): listening to the
        /// clock is one job, and every pending wait is one more until it runs or is cancelled.
        /// </param>
        public void Listen(Func<IDisposable> trackJob = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SimClockRunDelay));
            if (trackJob != null) _trackJob = trackJob;
            if (_listening != null) return;
            _listening = _clock.OnStep(SimRunClock.Owner, OnStep);
            _listeningJob = _trackJob?.Invoke();
        }

        /// <summary>The game left the screen: timed waits stand still on the steps that come (next-frame waits still run).</summary>
        public void Hold() => _held = true;

        /// <summary>The game is back: timed waits count again, keeping the time they had before leaving.</summary>
        public void Release() => _held = false;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var pending in _pending.ToArray()) pending.Dispose();
            _pending.Clear();
            _listening?.Dispose();
            _listening = null;
            _listeningJob?.Dispose();
            _listeningJob = null;
        }

        private IDisposable Add(Pending pending)
        {
            Listen();
            pending.Job = _trackJob?.Invoke();
            _pending.Add(pending);
            return pending;
        }

        private void OnStep(SimStep step)
        {
            if (_pending.Count == 0) return;
            // A wait added by a callback below starts on a later step.
            foreach (var pending in _pending.ToArray())
            {
                if (pending.Done) continue;
                if (pending.Seconds.HasValue)
                {
                    if (step.Number <= pending.AskedAtStep || _held) continue;
                    pending.Elapsed += step.Seconds;
                    if (pending.Elapsed + Tolerance < pending.Seconds.Value) continue;
                }
                pending.Dispose();
                pending.Then();
            }
        }

        private sealed class Pending : IDisposable
        {
            private readonly SimClockRunDelay _owner;

            public Pending(SimClockRunDelay owner, Action then, float? seconds, long askedAtStep)
            {
                _owner = owner;
                Then = then;
                Seconds = seconds;
                AskedAtStep = askedAtStep;
            }

            public Action Then { get; }
            public float? Seconds { get; }
            public long AskedAtStep { get; }
            public double Elapsed { get; set; }
            public bool Done { get; private set; }
            public IDisposable Job { get; set; }

            /// <summary>Ends the wait (ran or cancelled): no longer pending, its job no longer counted.</summary>
            public void Dispose()
            {
                if (Done) return;
                Done = true;
                _owner._pending.Remove(this);
                Job?.Dispose();
                Job = null;
            }
        }
    }
}
