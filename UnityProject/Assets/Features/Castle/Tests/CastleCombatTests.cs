using Game.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Game.Castle.Tests
{
    /// <summary>
    /// From PROTOTYPE the castle's attacks and the enemies' hits on the castle go through the shared combat
    /// (live/feature-combat/combat-tick/tick-values: combat_enabled, lightning_hits).
    /// </summary>
    public class CastleCombatTests
    {
        private CastleConfig _config;
        private FakeRunFlow _run;
        private FakeEnemyField _enemies;
        private FakeCombat _combat;
        private FakeEnemyCombat _enemyCombat;
        private CastleBody _body;
        private CastleSkills _skills;
        private CastleAutoShot _shot;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CastleConfig>();
            _run = new FakeRunFlow();
            _enemies = new FakeEnemyField();
            _combat = new FakeCombat();
            _enemyCombat = new FakeEnemyCombat();
            var hits = new CombatEnemyHits(_combat, _enemyCombat);
            _body = new CastleBody(_config, _run);
            _skills = new CastleSkills(_config, _body, _run, _enemies, new FakeHeroSquad(), hits);
            var upgrades = new CastleUpgrades(_body, _skills);
            _shot = new CastleAutoShot(_config, _body, _enemies, upgrades, hits);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Auto_shot_is_one_combat_hit_on_the_enemies_receiver_keyed_by_the_enemy_from_the_castle()
        {
            _enemies.AddEnemy(4, 3f);

            _shot.Tick(0.025f);

            Assert.That(_combat.Hits, Has.Count.EqualTo(1));
            var hit = _combat.Hits[0];
            Assert.That(hit.Receiver, Is.EqualTo(_enemyCombat.CombatReceiver));
            Assert.That(hit.TargetKey, Is.EqualTo(4));
            Assert.That(hit.Damage, Is.EqualTo(10f));
            Assert.That(hit.Source, Is.EqualTo(CombatSource.Castle));
        }

        [Test]
        public void Castle_never_takes_enemy_health_itself()
        {
            _enemies.AddEnemy(4, 3f);

            _shot.Tick(0.025f);
            _skills.TryUse(CastleSkill.Thunder);

            Assert.That(_enemies.Damaged, Is.Empty, "no IEnemyField.Damage: combat hands the enemies their totals");
        }

        [Test]
        public void Thunder_is_one_combat_hit_for_each_enemy_in_its_zone()
        {
            _enemies.AddEnemy(1, 3f);
            _enemies.AddEnemy(2, -4f);
            _enemies.AddEnemy(3, 12f); // beyond 6u from the edge

            _skills.TryUse(CastleSkill.Thunder);

            Assert.That(_combat.Hits, Has.Count.EqualTo(2));
            Assert.That(_combat.Hits.ConvertAll(h => h.TargetKey), Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(_combat.Hits.TrueForAll(h => h.Damage == 80f && h.Source == CombatSource.Castle
                && h.Receiver == _enemyCombat.CombatReceiver), Is.True);
        }

        [Test]
        public void Castle_takes_one_total_per_tick_and_reports_its_fall_only_after_every_owner_took_theirs()
        {
            Assert.That(_body.CanBeHit(0), Is.True);

            _body.TakeTotal(0, 1200f);
            Assert.That(_body.Health, Is.EqualTo(0f));
            Assert.That(_run.LoseCalls, Is.EqualTo(0), "nothing reported yet");
            Assert.That(_body.CanBeHit(0), Is.False);

            _body.ReportFallen();
            Assert.That(_body.IsDestroyed, Is.True);
            Assert.That(_run.LoseCalls, Is.EqualTo(1));

            _body.ReportFallen();
            Assert.That(_run.LoseCalls, Is.EqualTo(1), "reported once");
        }

        [Test]
        public void Total_that_leaves_health_above_zero_reports_no_fall()
        {
            _body.TakeTotal(0, 999.6f);
            _body.ReportFallen();

            Assert.That(_body.IsDestroyed, Is.False);
            Assert.That(_body.Health, Is.EqualTo(0.4f).Within(1e-3f));
            Assert.That(_run.LoseCalls, Is.EqualTo(0));
        }

        [Test]
        public void Health_bought_between_the_fatal_total_and_the_fall_report_is_not_applied()
        {
            _body.TakeTotal(0, 1000f);

            _body.ApplyHealthLevel(2);
            _body.ReportFallen();

            Assert.That(_body.MaxHealth, Is.EqualTo(1000f));
            Assert.That(_body.IsDestroyed, Is.True);
        }

        [Test]
        public void Castle_falling_in_combat_is_no_longer_a_foe_and_takes_no_more_totals()
        {
            var foes = new System.Collections.Generic.List<Game.Enemy.EnemyFoe>();
            _body.TakeTotal(0, 1000f);
            _body.ReportFallen();

            _body.GetFoes(foes);
            _body.TakeTotal(0, 50f);

            Assert.That(foes, Is.Empty);
            Assert.That(_body.Health, Is.EqualTo(0f));
        }

        [Test]
        public void Revived_castle_can_be_hit_through_combat_again()
        {
            _body.TakeTotal(0, 1000f);
            _body.ReportFallen();

            _body.Revive();

            Assert.That(_body.CanBeHit(0), Is.True);
            _body.TakeTotal(0, 100f);
            Assert.That(_body.Health, Is.EqualTo(200f));
        }

        [Test]
        public void Keys_other_than_the_castle_are_not_hit()
        {
            Assert.That(_body.CanBeHit(3), Is.False);
            _body.TakeTotal(3, 500f);
            Assert.That(_body.Health, Is.EqualTo(1000f));
        }
    }
}
