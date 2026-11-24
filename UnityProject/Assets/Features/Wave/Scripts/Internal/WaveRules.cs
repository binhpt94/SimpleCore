using System;
using System.Collections.Generic;

namespace Game.Wave
{
    /// <summary>Share of each enemy kind, in percent of the places left after the Boss, from wave <see cref="FromWave"/> on.</summary>
    internal readonly struct WaveMixTier
    {
        public WaveMixTier(int fromWave, int soldier, int wolf, int giant, int archer)
        {
            FromWave = fromWave;
            Soldier = soldier;
            Wolf = wolf;
            Giant = giant;
            Archer = archer;
        }

        public int FromWave { get; }
        public int Soldier { get; }
        public int Wolf { get; }
        public int Giant { get; }
        public int Archer { get; }
        public int Total => Soldier + Wolf + Giant + Archer;
    }

    /// <summary>
    /// Every number Wave plays by (wave-formula, spawn-order, clear-rules, gap-timer). Built from the config asset,
    /// or directly in tests. <see cref="Validate"/> throws on a broken config, naming what is wrong.
    /// </summary>
    internal sealed class WaveRules
    {
        public int BaseCount = 6;
        public int CountPerWave = 2;

        public float BaseInterval = 1.0f;
        public float IntervalStep = 0.05f;
        public float MinInterval = 0.3f;

        public float StatStepPerWave = 0.10f;

        public int WolfFromWave = 3;
        public int GiantFromWave = 5;
        public int ArcherFromWave = 7;
        public int BossEveryWaves = 10;

        /// <summary>False at IDEA stage: Archer places go to Soldiers.</summary>
        public bool UseArcher = true;

        /// <summary>False at IDEA stage: no Boss, its place goes to Soldiers.</summary>
        public bool UseBoss = true;

        public List<WaveMixTier> Tiers = DefaultTiers();

        public int ClearRewardBase = 20;
        public int ClearRewardPerWave = 5;

        public float GapSeconds = 3f;
        public int AliveCap = 40;
        public int MaxSameSideRun = 3;

        public static List<WaveMixTier> DefaultTiers() => new List<WaveMixTier>
        {
            new WaveMixTier(1, 100, 0, 0, 0),
            new WaveMixTier(3, 70, 30, 0, 0),
            new WaveMixTier(5, 55, 30, 15, 0),
            new WaveMixTier(7, 45, 25, 15, 15),
            new WaveMixTier(10, 40, 25, 20, 15),
            new WaveMixTier(15, 35, 25, 20, 20),
        };

        public void Validate()
        {
            if (BaseCount + CountPerWave <= 0) throw Bad("enemy count of wave 1 must be above 0");
            if ((BaseCount % 2) != 0 || (CountPerWave % 2) != 0)
                throw Bad("enemy count must stay even (base count and count per wave must be even) so each side gets half");
            if (MinInterval <= 0f) throw Bad("min spawn interval must be above 0");
            if (BaseInterval < MinInterval) throw Bad("base spawn interval must not be below the min interval");
            if (BossEveryWaves <= 0) throw Bad("boss cycle must be above 0");
            if (GapSeconds < 0f) throw Bad("break length must not be negative");
            if (AliveCap <= 0) throw Bad("alive cap must be above 0");
            if (MaxSameSideRun <= 0) throw Bad("max same side run must be above 0");
            if (Tiers == null || Tiers.Count == 0) throw Bad("mix tiers are missing");
            if (Tiers[0].FromWave != 1) throw Bad("the first mix tier must start at wave 1");
            for (var i = 0; i < Tiers.Count; i++)
            {
                var t = Tiers[i];
                if (i > 0 && t.FromWave <= Tiers[i - 1].FromWave)
                    throw Bad($"mix tier from wave {t.FromWave} must start after the tier before it");
                if (t.Soldier < 0 || t.Wolf < 0 || t.Giant < 0 || t.Archer < 0)
                    throw Bad($"mix tier from wave {t.FromWave} has a negative percent");
                if (t.Total != 100)
                    throw Bad($"mix tier from wave {t.FromWave} adds up to {t.Total}%, not 100%");
            }
        }

        private static InvalidOperationException Bad(string what) =>
            new InvalidOperationException("Wave config: " + what + ".");
    }
}
