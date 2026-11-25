using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.SimClock
{
    /// <summary>
    /// The one config of the shared clock and the seeded random (live: feature-sim-clock/clock-step/clock-values, table
    /// ClockParams; feature-sim-clock/seeded-random/random-values, tables SeedParams and Streams). The asset in
    /// FeatureAssets/Private holds the design numbers; the step order and the stream list are data, not code.
    /// </summary>
    internal sealed class SimClockConfig : ScriptableObject
    {
        [Header("ClockParams")]
        [Tooltip("tick_rate: steps a second. step_seconds is 1 / tick_rate.")]
        [SerializeField] internal int tickRate = 40;

        [Tooltip("default_mode: fixed_tick (FixedTick) or frame (Frame). Read once when a run is set up.")]
        [SerializeField] internal SimMode mode = SimMode.FixedTick;

        [Tooltip("max_steps_per_frame: most steps one frame runs at speed 1.")]
        [SerializeField] internal int maxStepsPerFrame = 5;

        [Tooltip("max_frame_seconds: longest frame time counted; the rest is dropped.")]
        [SerializeField] internal float maxFrameSeconds = 0.125f;

        [Tooltip("dev_speeds: fast-forward speeds of the dev build. dev_max_steps_per_frame = max_steps_per_frame x speed.")]
        [SerializeField] internal int[] devSpeeds = { 1, 2, 4, 8, 16 };

        [Tooltip("dev_run_steps: the dev build may run N steps at once.")]
        [SerializeField] internal bool devRunSteps = true;

        [Tooltip("dev_freeze_step: the dev build may freeze the clock and run one step.")]
        [SerializeField] internal bool devFreezeStep = true;

        [Tooltip("view_interpolation: views smooth between two steps (no at IDEA and PROTOTYPE).")]
        [SerializeField] internal bool viewInterpolation;

        [Tooltip("Owners called in each step, in this order (clock-step, Danh sách).")]
        [SerializeField] internal string[] stepOrder = new string[0];

        [Header("SeedParams")]
        [Tooltip("dev_seed_default: the seed every run of the dev build uses.")]
        [SerializeField] internal int devSeed = 12345;

        [Tooltip("seed_min")]
        [SerializeField] internal int seedMin;

        [Tooltip("seed_max")]
        [SerializeField] internal int seedMax = int.MaxValue;

        [Header("Streams")]
        [SerializeField] internal StreamEntry[] streams = new StreamEntry[0];

        /// <summary>step_seconds: the nominal step length.</summary>
        internal double StepSeconds => 1.0 / tickRate;

        /// <summary>dev_max_steps_per_frame at <paramref name="speed"/>.</summary>
        internal int MaxStepsAt(int speed) => maxStepsPerFrame * speed;

        /// <summary>Place of <paramref name="owner"/> in the step order, or -1.</summary>
        internal int OrderOf(string owner) => Array.IndexOf(stepOrder, owner);

        /// <summary>The row of the Streams table called <paramref name="name"/> (id or display name, any case), or null.</summary>
        internal StreamEntry FindStream(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var s in streams)
            {
                if (s == null) continue;
                if (string.Equals(s.id, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s.name, name, StringComparison.OrdinalIgnoreCase)) return s;
            }
            return null;
        }

        /// <summary>Throws with every problem found, naming the field, so a broken config stops the boot.</summary>
        internal void Validate()
        {
            var problems = new List<string>();
            if (tickRate <= 0) problems.Add("tick_rate must be above 0");
            if (maxStepsPerFrame <= 0) problems.Add("max_steps_per_frame must be above 0");
            if (maxFrameSeconds <= 0) problems.Add("max_frame_seconds must be above 0");
            if (devSpeeds == null || devSpeeds.Length == 0) problems.Add("dev_speeds is empty");
            else
                foreach (var s in devSpeeds)
                    if (s <= 0) problems.Add($"dev_speeds has {s}; speeds must be above 0");
            if (stepOrder == null || stepOrder.Length == 0) problems.Add("step order is empty");
            else
            {
                var seen = new HashSet<string>();
                foreach (var o in stepOrder)
                {
                    if (string.IsNullOrEmpty(o)) problems.Add("step order has an empty owner");
                    else if (!seen.Add(o)) problems.Add($"step order lists '{o}' twice");
                }
            }
            if (seedMin < 0) problems.Add("seed_min must be 0 or more");
            if (seedMax < seedMin) problems.Add("seed_max must be at least seed_min");
            if (devSeed < seedMin || devSeed > seedMax)
                problems.Add($"dev_seed {devSeed} is outside seed_min {seedMin} .. seed_max {seedMax}");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in streams ?? new StreamEntry[0])
            {
                if (s == null || string.IsNullOrEmpty(s.id)) problems.Add("Streams has a row without id");
                else if (!ids.Add(s.id)) problems.Add($"Streams lists '{s.id}' twice");
            }
            if (problems.Count > 0)
                throw new InvalidOperationException($"SimClock config '{name}': " + string.Join("; ", problems));
        }

        /// <summary>One row of the Streams table.</summary>
        [Serializable]
        internal sealed class StreamEntry
        {
            public string id;
            public string name;
            public string stage;
            public string owner;
            public string uses;
        }
    }
}
