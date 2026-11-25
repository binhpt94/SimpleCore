using System.Collections.Generic;
using Game.Combat;
using Game.Run;
using NUnit.Framework;

namespace Game.Hero.Tests
{
    /// <summary>
    /// From PROTOTYPE every hit goes through the shared combat (live feature-combat/combat-tick/tick-values, combat_enabled):
    /// the heroes hand their hits to combat instead of taking the enemies' health, and take their own health once per tick
    /// from the total combat hands back. Also the down state other features read (IHeroDownState).
    /// </summary>
    public sealed class HeroCombatTests
    {
        private const float Tolerance = 1e-4f;

        private HeroFakes.EnemyField _field;
        private HeroFakes.EnemyCombat _enemyCombat;
        private HeroFakes.Combat _combat;
        private HeroSquad _squad;
        private int _heroReceiver;
        private readonly List<int> _downChanges = new List<int>();

        [SetUp]
        public void SetUp()
        {
            _field = new HeroFakes.EnemyField();
            _enemyCombat = new HeroFakes.EnemyCombat();
            _combat = new HeroFakes.Combat();
            _squad = new HeroSquad(HeroFakes.LoadConfig(), _field, new CombatHeroHits(_combat, _enemyCombat));
            _heroReceiver = _combat.AddReceiver(_squad);
            _downChanges.Clear();
            _squad.DownChanged += slot => _downChanges.Add(slot);
        }

        private void StartWithOnlySlot1Standing(string slot1)
        {
            _squad.Begin(new RunTeam(new[] { slot1, slot1 == "knight" ? "axeman" : "knight", "archer" }));
            _squad.Hit(1, 1000f);
            _squad.Hit(2, 1000f);
            _downChanges.Clear();
        }

        private void EnemyHitsHero(int slot, float damage) =>
            _combat.Hit(new CombatHit(_heroReceiver, slot, damage, CombatSource.Enemy));

        // ------------------------------------------------------------------ heroes' hits

        [Test]
        public void A_hero_hit_goes_to_combat_and_takes_no_enemy_health_itself()
        {
            StartWithOnlySlot1Standing("knight");
            var enemy = _field.Add(0f);

            _squad.Tick(0.025f);

            Assert.That(_combat.Hits, Has.Count.EqualTo(1));
            var hit = _combat.Hits[0];
            Assert.That(hit.Receiver, Is.EqualTo(HeroFakes.EnemyCombat.Receiver), "the enemies' combat number");
            Assert.That(hit.TargetKey, Is.EqualTo(enemy));
            Assert.That(hit.Damage, Is.EqualTo(12f).Within(Tolerance));
            Assert.That(hit.Source, Is.EqualTo(CombatSource.Hero));
            Assert.That(_field.Taken(enemy), Is.EqualTo(0f), "combat, not the hero, takes the enemy's health");
        }

        [Test]
        public void An_area_hit_is_one_combat_hit_per_enemy_it_reaches()
        {
            StartWithOnlySlot1Standing("axeman");
            var target = _field.Add(-0.2f);
            var inStretch = _field.Add(0.8f);
            var pastStretch = _field.Add(1.0f);

            _squad.Tick(0.025f);

            var keys = new List<int>();
            foreach (var hit in _combat.Hits) keys.Add(hit.TargetKey);
            Assert.That(keys, Is.EquivalentTo(new[] { target, inStretch }));
            Assert.That(keys, Has.No.Member(pastStretch));
            Assert.That(_combat.Hits.TrueForAll(h => System.Math.Abs(h.Damage - 14f) < Tolerance), Is.True);
        }

        [Test]
        public void Two_heroes_hitting_the_same_enemy_in_one_step_both_hand_their_hit_to_combat()
        {
            _squad.Begin(new RunTeam(new[] { "knight", "archer", "axeman" }));
            _squad.Hit(2, 1000f);                  // axeman out of the way
            var enemy = _field.Add(0f, health: 5f); // one hit would defeat it

            _squad.Tick(0.025f);

            Assert.That(_combat.Hits.FindAll(h => h.TargetKey == enemy), Has.Count.EqualTo(2),
                "nobody is defeated before combat settles the step, so both heroes hit it");
        }

        // ------------------------------------------------------------------ hits on the heroes

        [Test]
        public void Hits_on_a_hero_in_one_tick_are_taken_as_one_total()
        {
            StartWithOnlySlot1Standing("knight");
            var hurts = 0;
            _squad.Hurt += _ => hurts++;

            EnemyHitsHero(0, 100f);
            EnemyHitsHero(0, 50f);
            _combat.Settle();

            Assert.That(_squad.Units[0].Health, Is.EqualTo(150f).Within(Tolerance));
            Assert.That(hurts, Is.EqualTo(1), "one total, one hurt");
        }

        [Test]
        public void A_hero_emptied_by_the_total_falls_only_when_combat_reports_the_fallen()
        {
            StartWithOnlySlot1Standing("knight");
            var fell = 0;
            _squad.Fell += _ => fell++;

            _squad.TakeTotal(0, 1000f);
            Assert.That(_squad.Units[0].Health, Is.EqualTo(0f));
            Assert.That(_squad.Units[0].IsStanding, Is.True, "not reported yet");
            Assert.That(fell, Is.EqualTo(0));

            _squad.ReportFallen();
            _squad.ReportFallen();

            Assert.That(_squad.Units[0].IsStanding, Is.False);
            Assert.That(_squad.Units[0].RespawnLeft, Is.EqualTo(8f).Within(Tolerance));
            Assert.That(fell, Is.EqualTo(1), "reported once");
        }

        [Test]
        public void A_fallen_or_growing_hero_cannot_be_hit_through_combat()
        {
            StartWithOnlySlot1Standing("knight");

            Assert.That(_squad.CanBeHit(0), Is.True);
            Assert.That(_squad.CanBeHit(1), Is.False, "fallen");
            Assert.That(_squad.CanBeHit(9), Is.False, "no such slot");

            for (var i = 0; i < 320; i++) _squad.Tick(0.025f);   // slot 2 respawns and starts growing back
            Assert.That(_squad.Units[1].IsStanding, Is.True);
            Assert.That(_squad.CanBeHit(1), Is.False, "growing back");

            EnemyHitsHero(1, 50f);
            _combat.Settle();
            Assert.That(_squad.Units[1].Health, Is.EqualTo(240f).Within(Tolerance), "the hit on a growing hero is dropped");
        }

        // ------------------------------------------------------------------ IHeroDownState

        [Test]
        public void A_hero_is_down_from_its_fall_until_it_respawns_and_each_change_is_raised_once()
        {
            StartWithOnlySlot1Standing("knight");
            Assert.That(_squad.IsDown(0), Is.False);
            Assert.That(_squad.IsDown(1), Is.True);

            EnemyHitsHero(0, 1000f);
            _combat.Settle();
            Assert.That(_squad.IsDown(0), Is.True);
            Assert.That(_downChanges, Is.EqualTo(new[] { 0 }), "went down");

            _downChanges.Clear();
            for (var i = 0; i < 320; i++) _squad.Tick(0.025f);
            Assert.That(_squad.IsDown(0), Is.False);
            Assert.That(_downChanges, Has.Member(0), "stood up again");
            Assert.That(_downChanges.FindAll(s => s == 0), Has.Count.EqualTo(1));
        }

        [Test]
        public void A_new_run_or_leaving_the_battle_clears_every_down_mark()
        {
            StartWithOnlySlot1Standing("knight");   // slots 2 and 3 are down

            _squad.Begin(new RunTeam(new[] { "knight", "axeman", "archer" }));

            Assert.That(_downChanges, Is.EquivalentTo(new[] { 1, 2 }), "a new run raises every slot that was down");
            Assert.That(_squad.IsDown(1), Is.False);
            Assert.That(_squad.IsDown(2), Is.False);

            _squad.Hit(0, 1000f);
            _downChanges.Clear();
            _squad.Clear();

            Assert.That(_downChanges, Is.EqualTo(new[] { 0 }), "leaving the battle raises the slot that was down");
            Assert.That(_squad.IsDown(0), Is.False, "no run going: nobody is down");
        }
    }
}
