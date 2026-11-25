using System;

namespace Game.Combat
{
    /// <summary>
    /// Turns frame time into a whole number of fixed steps, at most a set number per frame. Time beyond that cap
    /// is dropped: on a very slow device the battle runs slower instead of jumping ahead.
    /// </summary>
    internal sealed class FixedStepClock
    {
        // Absorbs float error so 0.02 + 0.02 + ... counts whole steps.
        private const double Tolerance = 1e-6;

        private readonly int _maxStepsPerFrame;
        private readonly double _step;
        private double _carry;

        public FixedStepClock(CombatSettings settings)
        {
            if (!(settings.StepSeconds > 0f))
                throw new ArgumentException($"Combat: step must be above 0 seconds, got {settings.StepSeconds}.", nameof(settings));
            if (settings.MaxStepsPerFrame < 1)
                throw new ArgumentException($"Combat: max steps per frame must be 1 or more, got {settings.MaxStepsPerFrame}.", nameof(settings));
            StepSeconds = settings.StepSeconds;
            _step = settings.StepSeconds;
            _maxStepsPerFrame = settings.MaxStepsPerFrame;
        }

        public float StepSeconds { get; }

        /// <summary>Adds one frame of time; returns how many steps to run for it.</summary>
        public int Advance(float frameSeconds)
        {
            if (!(frameSeconds > 0f)) return 0;
            _carry += frameSeconds;
            var steps = (int)Math.Floor(_carry / _step + Tolerance);
            if (steps > _maxStepsPerFrame)
            {
                _carry = 0;
                return _maxStepsPerFrame;
            }
            _carry = Math.Max(0, _carry - steps * _step);
            return steps;
        }

        /// <summary>Forgets time not yet turned into a step.</summary>
        public void Reset() => _carry = 0;
    }
}
