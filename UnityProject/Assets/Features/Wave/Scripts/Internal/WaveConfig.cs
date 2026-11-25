using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Wave
{
    /// <summary>Balance numbers of the waves (values decided by feature-balance). Lives in FeatureAssets/Private.</summary>
    [CreateAssetMenu(menuName = "Game/Wave/Wave Config", fileName = "WaveConfig")]
    internal sealed class WaveConfig : ScriptableObject
    {
        [Serializable]
        internal struct MixTier
        {
            [Min(1)] public int fromWave;
            [Range(0, 100)] public int soldier;
            [Range(0, 100)] public int wolf;
            [Range(0, 100)] public int giant;
            [Range(0, 100)] public int archer;
        }

        [Header("Enemy count: base + perWave × w")]
        [SerializeField] private int baseCount = 6;
        [SerializeField] private int countPerWave = 2;

        [Header("Spawn interval (s): max(min, base − step × (w − 1))")]
        [SerializeField] private float baseInterval = 1.0f;
        [SerializeField] private float intervalStep = 0.05f;
        [SerializeField] private float minInterval = 0.3f;

        [Header("Health and damage × (1 + step × (w − 1))")]
        [SerializeField] private float statStepPerWave = 0.10f;

        [Header("Kinds unlock at wave")]
        [SerializeField] private int wolfFromWave = 3;
        [SerializeField] private int giantFromWave = 5;
        [SerializeField] private int archerFromWave = 7;
        [SerializeField] private int bossEveryWaves = 10;

        [Tooltip("PROTOTYPE and FULL: on (all 5 kinds). IDEA stage was off: Archer places went to Soldiers.")]
        [SerializeField] private bool useArcher = true;

        [Tooltip("PROTOTYPE and FULL: on, one Boss every bossEveryWaves waves, last of the wave. IDEA stage was off.")]
        [SerializeField] private bool useBoss = true;

        [Header("Kind mix (% of places after the Boss), each tier adds up to 100")]
        [SerializeField] private List<MixTier> tiers = new List<MixTier>
        {
            new MixTier { fromWave = 1, soldier = 100 },
            new MixTier { fromWave = 3, soldier = 70, wolf = 30 },
            new MixTier { fromWave = 5, soldier = 55, wolf = 30, giant = 15 },
            new MixTier { fromWave = 7, soldier = 45, wolf = 25, giant = 15, archer = 15 },
            new MixTier { fromWave = 10, soldier = 40, wolf = 25, giant = 20, archer = 15 },
            new MixTier { fromWave = 15, soldier = 35, wolf = 25, giant = 20, archer = 20 },
        };

        [Header("Clear reward (coins): base + perWave × w")]
        [SerializeField] private int clearRewardBase = 20;
        [SerializeField] private int clearRewardPerWave = 5;

        [Header("Sending")]
        [SerializeField] private float gapSeconds = 3f;
        [SerializeField] private int aliveCap = 40;
        [SerializeField] private int maxSameSideRun = 3;

        /// <summary>The rules this asset describes; throws when the numbers do not hold together.</summary>
        public WaveRules ToRules()
        {
            var rules = new WaveRules
            {
                BaseCount = baseCount,
                CountPerWave = countPerWave,
                BaseInterval = baseInterval,
                IntervalStep = intervalStep,
                MinInterval = minInterval,
                StatStepPerWave = statStepPerWave,
                WolfFromWave = wolfFromWave,
                GiantFromWave = giantFromWave,
                ArcherFromWave = archerFromWave,
                BossEveryWaves = bossEveryWaves,
                UseArcher = useArcher,
                UseBoss = useBoss,
                Tiers = new List<WaveMixTier>(),
                ClearRewardBase = clearRewardBase,
                ClearRewardPerWave = clearRewardPerWave,
                GapSeconds = gapSeconds,
                AliveCap = aliveCap,
                MaxSameSideRun = maxSameSideRun,
            };
            if (tiers != null)
            {
                foreach (var t in tiers) rules.Tiers.Add(new WaveMixTier(t.fromWave, t.soldier, t.wolf, t.giant, t.archer));
            }
            rules.Validate();
            return rules;
        }
    }
}
