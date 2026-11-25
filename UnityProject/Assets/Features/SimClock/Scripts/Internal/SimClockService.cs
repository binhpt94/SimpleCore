using System;
using System.Collections.Generic;

namespace Game.SimClock
{
    /// <summary>
    /// The shared clock, the run milestones and the seeded random of one scene (feature-sim-clock). Plain C#: the
    /// frame loop only calls <see cref="Advance"/> with the real frame time.
    /// <para>
    /// Per step: the step number goes up, the queued player actions are applied in the order queued (recorded with
    /// that number), then every subscriber runs in the config's owner order. The next step starts only after the
    /// previous one ran every subscriber, since everything is called one after the other.
    /// </para>
    /// </summary>
    internal sealed class SimClockService : ISimClock, ISimRunControl, ISimRandom
    {
        private const double Epsilon = 1e-9;

        private readonly SimClockConfig _config;
        private readonly bool _dev;
        private readonly ISeedSource _seeds;
        private readonly ISeedLog _log;

        private readonly List<Subscriber> _subscribers = new List<Subscriber>();
        private readonly List<QueuedAction> _queue = new List<QueuedAction>();
        private readonly List<AppliedAction> _applied = new List<AppliedAction>();
        private readonly Dictionary<string, SimRandomStream> _streams =
            new Dictionary<string, SimRandomStream>(StringComparer.OrdinalIgnoreCase);

        private SimMode _mode;
        private double _step;
        private int _maxSteps;
        private double _maxFrame;

        private bool _setUp;
        private bool _started;
        private bool _seedRecorded;
        private bool _gameplay;
        private bool _stepping;
        private long _steps;
        private long _gameplaySteps;
        private double _frameModeSeconds;
        private double _leftover;
        private int _seed;
        private long _nextSubscriberId;

        private int _speed = 1;
        private bool _frozen;

        internal SimClockService(SimClockConfig config, bool dev, ISeedSource seeds, ISeedLog log)
        {
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _dev = dev;
            _seeds = seeds ?? throw new ArgumentNullException(nameof(seeds));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            ReadConfig();
        }

        // ---------------------------------------------------------------- ISimClock

        public SimMode Mode => _mode;

        public float StepSeconds => (float)_step;

        public long StepCount => _steps;

        public long GameplayStepCount => _gameplaySteps;

        /// <summary>Fixed mode: gameplay steps x step length, counted in whole steps so long runs never drift.</summary>
        public double GameplaySeconds => _mode == SimMode.FixedTick ? _gameplaySteps * _step : _frameModeSeconds;

        public IDisposable OnStep(string owner, Action<SimStep> onStep)
        {
            if (onStep == null) throw new ArgumentNullException(nameof(onStep));
            var order = _config.OrderOf(owner);
            if (order < 0)
                throw new ArgumentException(
                    $"'{owner}' is not in the step order of the SimClock config ({string.Join(", ", _config.stepOrder)}).",
                    nameof(owner));
            var sub = new Subscriber(this, owner, order, _nextSubscriberId++, onStep);
            var at = _subscribers.FindIndex(s => s.Order > order);
            if (at < 0) _subscribers.Add(sub);
            else _subscribers.Insert(at, sub);
            return sub;
        }

        public void Enqueue(string owner, Action apply)
        {
            if (string.IsNullOrEmpty(owner)) throw new ArgumentException("An action needs its owner's name.", nameof(owner));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            _queue.Add(new QueuedAction(owner, apply));
        }

        // ---------------------------------------------------------------- ISimRunControl

        public int Seed => _seed;

        public void SetUpRun()
        {
            ReadConfig();
            _steps = 0;
            _gameplaySteps = 0;
            _frameModeSeconds = 0;
            _leftover = 0;
            _queue.Clear();
            _applied.Clear();
            _started = false;
            _seedRecorded = false;
            _gameplay = false;
            _seed = _dev ? _config.devSeed : _seeds.Next(_config.seedMin, _config.seedMax);
            if (_seed < _config.seedMin || _seed > _config.seedMax)
                throw new InvalidOperationException(
                    $"Seed {_seed} is outside seed_min {_config.seedMin} .. seed_max {_config.seedMax}.");
            foreach (var s in _streams.Values) s.Reset(_seed);
            _setUp = true;
        }

        public void StartRun()
        {
            if (!_setUp) throw new InvalidOperationException("StartRun before SetUpRun: set the run up first.");
            if (_started) return;
            _started = true;
            _gameplay = true;
            if (_seedRecorded) return;
            _seedRecorded = true;
            _log.Write(_dev
                ? $"[SimClock] Seed của lượt (bản dev, từ config): {_seed}"
                : $"[SimClock] Seed của lượt: {_seed}");
        }

        public void SetGameplay(bool playing) => _gameplay = playing;

        public void ClearRun()
        {
            _queue.Clear();
            _leftover = 0;
            foreach (var s in _streams.Values) s.Clear();
            _setUp = false;
            _started = false;
            _gameplay = false;
        }

        // ---------------------------------------------------------------- ISimRandom

        public ISimRandomStream Stream(string name)
        {
            var entry = _config.FindStream(name);
            if (entry == null)
                throw new InvalidOperationException(
                    $"No random stream '{name}' in the Streams table of the SimClock config; add a row for it " +
                    "(live: feature-sim-clock/seeded-random/random-values).");
            if (_streams.TryGetValue(entry.id, out var stream)) return stream;
            stream = new SimRandomStream(entry.id);
            if (_setUp) stream.Reset(_seed);
            _streams.Add(entry.id, stream);
            return stream;
        }

        // ---------------------------------------------------------------- frames

        /// <summary>True between StartRun and ClearRun: steps may run.</summary>
        internal bool Running => _started;

        /// <summary>Whether the coming steps are gameplay steps.</summary>
        internal bool Gameplay => _gameplay;

        /// <summary>Time kept for the next frame (fixed mode), in seconds.</summary>
        internal double Leftover => _leftover;

        /// <summary>Every applied action of the run with the step it was applied at, in order (for replay).</summary>
        internal IReadOnlyList<AppliedAction> Applied => _applied;

        internal int Speed => _speed;

        internal bool Frozen => _frozen;

        /// <summary>One frame of <paramref name="frameSeconds"/> real seconds. Returns the steps it ran.</summary>
        internal int Advance(double frameSeconds)
        {
            if (!_started || _frozen || !(frameSeconds > 0)) return 0;
            var frame = Math.Min(frameSeconds, _maxFrame);
            if (_mode == SimMode.Frame)
            {
                var ran = 0;
                for (var i = 0; i < _speed && _started; i++, ran++) RunStep(frame);
                return ran;
            }

            _leftover += frame * _speed;
            var cap = _maxSteps * _speed;
            var n = 0;
            while (n < cap && _started && _leftover + Epsilon >= _step)
            {
                _leftover -= _step;
                RunStep(_step);
                n++;
            }
            if (_leftover + Epsilon >= _step) _leftover -= Math.Floor((_leftover + Epsilon) / _step) * _step;
            if (_leftover < 0) _leftover = 0;
            return n;
        }

        private void RunStep(double seconds)
        {
            if (_stepping) throw new InvalidOperationException("A step may not start inside another step.");
            _stepping = true;
            try
            {
                _steps++;
                if (_queue.Count > 0)
                {
                    var batch = _queue.ToArray();
                    _queue.Clear();
                    foreach (var q in batch)
                    {
                        _applied.Add(new AppliedAction(_steps, q.Owner));
                        q.Apply();
                    }
                }

                var gameplay = _gameplay;
                if (gameplay)
                {
                    _gameplaySteps++;
                    _frameModeSeconds += seconds;
                }
                var step = new SimStep(_steps, _gameplaySteps, (float)seconds, gameplay);
                foreach (var sub in _subscribers.ToArray())
                {
                    if (!sub.Disposed) sub.Callback(step);
                }
            }
            finally
            {
                _stepping = false;
            }
        }

        private void ReadConfig()
        {
            _mode = _config.mode;
            _step = _config.StepSeconds;
            _maxSteps = _config.maxStepsPerFrame;
            _maxFrame = _config.maxFrameSeconds;
        }

        // ---------------------------------------------------------------- dev build only (clock-values#dev-*)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Fast-forward: one of dev_speeds; the step cap of a frame grows with it.</summary>
        internal void SetSpeed(int speed)
        {
            if (Array.IndexOf(_config.devSpeeds, speed) < 0)
                throw new ArgumentOutOfRangeException(nameof(speed), speed,
                    $"Speed must be one of dev_speeds ({string.Join(", ", _config.devSpeeds)}).");
            _speed = speed;
        }

        /// <summary>Frozen: frames run no step until unfrozen; <see cref="StepOnce"/> still runs one.</summary>
        internal void Freeze(bool frozen)
        {
            if (!_config.devFreezeStep) throw new InvalidOperationException("dev_freeze_step is off in the SimClock config.");
            _frozen = frozen;
        }

        internal void StepOnce()
        {
            if (!_config.devFreezeStep) throw new InvalidOperationException("dev_freeze_step is off in the SimClock config.");
            RunNow(1);
        }

        /// <summary>Runs <paramref name="count"/> nominal steps now, without waiting for frames.</summary>
        internal void RunSteps(int count)
        {
            if (!_config.devRunSteps) throw new InvalidOperationException("dev_run_steps is off in the SimClock config.");
            RunNow(count);
        }

        private void RunNow(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (!_started) throw new InvalidOperationException("No run has started: steps run only between StartRun and ClearRun.");
            for (var i = 0; i < count && _started; i++) RunStep(_step);
        }
#endif

        // ---------------------------------------------------------------- types

        /// <summary>A player action as applied: which step, whose.</summary>
        internal readonly struct AppliedAction
        {
            internal AppliedAction(long step, string owner)
            {
                Step = step;
                Owner = owner;
            }

            internal long Step { get; }

            internal string Owner { get; }
        }

        private readonly struct QueuedAction
        {
            internal QueuedAction(string owner, Action apply)
            {
                Owner = owner;
                Apply = apply;
            }

            internal string Owner { get; }

            internal Action Apply { get; }
        }

        private sealed class Subscriber : IDisposable
        {
            private readonly SimClockService _clock;

            internal Subscriber(SimClockService clock, string owner, int order, long id, Action<SimStep> callback)
            {
                _clock = clock;
                Owner = owner;
                Order = order;
                Id = id;
                Callback = callback;
            }

            internal string Owner { get; }

            internal int Order { get; }

            internal long Id { get; }

            internal Action<SimStep> Callback { get; }

            internal bool Disposed { get; private set; }

            public void Dispose()
            {
                if (Disposed) return;
                Disposed = true;
                _clock._subscribers.Remove(this);
            }
        }
    }
}
