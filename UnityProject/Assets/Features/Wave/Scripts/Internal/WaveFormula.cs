using System;

namespace Game.Wave
{
    /// <summary>What wave <see cref="Number"/> is made of: how many of each kind, how fast they come, how strong.</summary>
    internal readonly struct WavePlan
    {
        public WavePlan(int number, float interval, float statScale, int soldiers, int wolves, int giants, int archers, int bosses)
        {
            Number = number;
            Interval = interval;
            StatScale = statScale;
            Soldiers = soldiers;
            Wolves = wolves;
            Giants = giants;
            Archers = archers;
            Bosses = bosses;
        }

        public int Number { get; }

        /// <summary>Seconds between two enemies of the wave.</summary>
        public float Interval { get; }

        /// <summary>Multiplies base health and damage of every enemy of the wave.</summary>
        public float StatScale { get; }

        public int Soldiers { get; }
        public int Wolves { get; }
        public int Giants { get; }
        public int Archers { get; }
        public int Bosses { get; }
        public int Count => Soldiers + Wolves + Giants + Archers + Bosses;
    }

    /// <summary>wave-formula: every value of wave w comes from w and the rules, no hand-written waves.</summary>
    internal sealed class WaveFormula
    {
        private readonly WaveRules _rules;

        public WaveFormula(WaveRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public WavePlan Plan(int wave)
        {
            if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave), "Waves are numbered from 1.");

            var count = _rules.BaseCount + _rules.CountPerWave * wave;
            var interval = Math.Max(_rules.MinInterval, _rules.BaseInterval - _rules.IntervalStep * (wave - 1));
            var scale = 1f + _rules.StatStepPerWave * (wave - 1);

            var bosses = _rules.UseBoss && wave % _rules.BossEveryWaves == 0 ? 1 : 0;
            var places = count - bosses;
            var tier = TierOf(wave);

            var wolves = Share(wave >= _rules.WolfFromWave, tier.Wolf, places);
            var giants = Share(wave >= _rules.GiantFromWave, tier.Giant, places);
            var archers = Share(_rules.UseArcher && wave >= _rules.ArcherFromWave, tier.Archer, places);

            // Hard kinds may not take every place: keep at least one Soldier, trimming the lowest share first.
            while (wolves + giants + archers > places - 1 && wolves + giants + archers > 0)
            {
                if (archers > 0 && (tier.Archer <= tier.Giant || giants == 0) && (tier.Archer <= tier.Wolf || wolves == 0)) archers--;
                else if (giants > 0 && (tier.Giant <= tier.Wolf || wolves == 0)) giants--;
                else wolves--;
            }

            var soldiers = places - wolves - giants - archers;
            return new WavePlan(wave, interval, scale, soldiers, wolves, giants, archers, bosses);
        }

        /// <summary>Whole part of percent × places, at least 1 for a kind already unlocked.</summary>
        private static int Share(bool unlocked, int percent, int places)
        {
            if (!unlocked) return 0;
            return Math.Max(1, percent * places / 100);
        }

        private WaveMixTier TierOf(int wave)
        {
            var found = _rules.Tiers[0];
            foreach (var tier in _rules.Tiers)
            {
                if (tier.FromWave <= wave) found = tier;
                else break;
            }
            return found;
        }
    }
}
