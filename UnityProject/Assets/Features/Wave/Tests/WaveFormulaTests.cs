using System;
using NUnit.Framework;

namespace Game.Wave.Tests
{
    public class WaveFormulaTests
    {
        private static WaveFormula Full() => new WaveFormula(new WaveRules());

        // wave, count, interval, scale, soldier, wolf, giant, archer, boss (example table of wave-formula)
        [TestCase(1, 8, 1.0f, 1.0f, 8, 0, 0, 0, 0)]
        [TestCase(3, 12, 0.9f, 1.2f, 9, 3, 0, 0, 0)]
        [TestCase(5, 16, 0.8f, 1.4f, 10, 4, 2, 0, 0)]
        [TestCase(7, 20, 0.7f, 1.6f, 9, 5, 3, 3, 0)]
        [TestCase(10, 26, 0.55f, 1.9f, 11, 6, 5, 3, 1)]
        [TestCase(15, 36, 0.3f, 2.4f, 13, 9, 7, 7, 0)]
        [TestCase(20, 46, 0.3f, 2.9f, 16, 11, 9, 9, 1)]
        public void Wave_IsMadeOfTheDesignedMix(int wave, int count, float interval, float scale,
            int soldiers, int wolves, int giants, int archers, int bosses)
        {
            var plan = Full().Plan(wave);

            Assert.That(plan.Count, Is.EqualTo(count));
            Assert.That(plan.Interval, Is.EqualTo(interval).Within(1e-4));
            Assert.That(plan.StatScale, Is.EqualTo(scale).Within(1e-4));
            Assert.That(plan.Soldiers, Is.EqualTo(soldiers), "soldiers");
            Assert.That(plan.Wolves, Is.EqualTo(wolves), "wolves");
            Assert.That(plan.Giants, Is.EqualTo(giants), "giants");
            Assert.That(plan.Archers, Is.EqualTo(archers), "archers");
            Assert.That(plan.Bosses, Is.EqualTo(bosses), "bosses");
        }

        [Test]
        public void NewlyUnlockedKind_AlwaysShowsUpAtLeastOnce()
        {
            var formula = Full();

            Assert.That(formula.Plan(3).Wolves, Is.GreaterThanOrEqualTo(1));
            Assert.That(formula.Plan(5).Giants, Is.GreaterThanOrEqualTo(1));
            Assert.That(formula.Plan(7).Archers, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void VeryHighWave_StillAddsUpAndKeepsTheIntervalFloor()
        {
            var plan = Full().Plan(100);

            Assert.That(plan.Count, Is.EqualTo(206));
            Assert.That(plan.Interval, Is.EqualTo(0.3f).Within(1e-4));
            Assert.That(plan.Soldiers, Is.GreaterThanOrEqualTo(1));
            Assert.That(plan.Bosses, Is.EqualTo(1));
        }

        [Test]
        public void IdeaStage_HasNoBossNorArcher_TheirPlacesGoToSoldiers()
        {
            var formula = new WaveFormula(new WaveRules { UseArcher = false, UseBoss = false });

            var plan = formula.Plan(10);

            Assert.That(plan.Count, Is.EqualTo(26));
            Assert.That(plan.Bosses, Is.EqualTo(0));
            Assert.That(plan.Archers, Is.EqualTo(0));
            Assert.That(plan.Wolves, Is.EqualTo(6));
            Assert.That(plan.Giants, Is.EqualTo(5));
            Assert.That(plan.Soldiers, Is.EqualTo(15));
        }

        [Test]
        public void UnlockedKindWhoseShareRoundsDownToZero_StillGetsOne_TakenFromSoldiers()
        {
            var rules = new WaveRules();
            rules.Tiers[1] = new WaveMixTier(3, 95, 5, 0, 0);

            var plan = new WaveFormula(rules).Plan(3);

            Assert.That(plan.Wolves, Is.EqualTo(1), "5% of 12 places is 0.6");
            Assert.That(plan.Soldiers, Is.EqualTo(11));
            Assert.That(plan.Count, Is.EqualTo(12));
        }

        [Test]
        public void HardKindsTakingEveryPlace_AreTrimmedFromTheLowestShare_KeepingOneSoldier()
        {
            var rules = new WaveRules { WolfFromWave = 1, GiantFromWave = 1, ArcherFromWave = 1 };
            rules.Tiers[0] = new WaveMixTier(1, 0, 50, 49, 1);

            var plan = new WaveFormula(rules).Plan(1);

            Assert.That(plan.Soldiers, Is.EqualTo(1));
            Assert.That(plan.Wolves, Is.EqualTo(4));
            Assert.That(plan.Giants, Is.EqualTo(3));
            Assert.That(plan.Archers, Is.EqualTo(0), "the 1% kind is the one trimmed");
            Assert.That(plan.Count, Is.EqualTo(8));
        }

        [TestCase(2, 1.1f)]
        [TestCase(16, 2.5f)]
        public void HealthAndDamageScale_StaysFractional_NotRounded(int wave, float scale)
        {
            Assert.That(Full().Plan(wave).StatScale, Is.EqualTo(scale).Within(1e-5));
        }

        [Test]
        public void BossWave_TakesOnePlace_TheOtherKindsShareTheRest()
        {
            var plan = Full().Plan(10);

            Assert.That(plan.Bosses, Is.EqualTo(1));
            Assert.That(plan.Count - plan.Bosses, Is.EqualTo(25));
            Assert.That(Full().Plan(9).Bosses, Is.EqualTo(0));
            Assert.That(Full().Plan(11).Bosses, Is.EqualTo(0));
        }

        [Test]
        public void ConfigWhoseTierDoesNotAddUpTo100_IsRefusedNamingTheTier()
        {
            var rules = new WaveRules();
            rules.Tiers[2] = new WaveMixTier(5, 50, 30, 15, 0);

            var error = Assert.Throws<InvalidOperationException>(rules.Validate);

            Assert.That(error.Message, Does.Contain("wave 5").And.Contain("95%"));
        }
    }
}
