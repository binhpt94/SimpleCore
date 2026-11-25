using System.Collections.Generic;
using Game.Enemy;
using Game.Run;
using NUnit.Framework;

namespace Game.Hero.Tests
{
    /// <summary>
    /// Hero rules with the real HeroConfig numbers (live: hero-roster-values, respawn-values, hero-level-values,
    /// targeting-values). Hero box 1u wide; fake enemies 0.8u wide unless said.
    /// </summary>
    public sealed class HeroSquadTests
    {
        private HeroFakes.EnemyField _field;
        private HeroSquad _squad;

        [SetUp]
        public void SetUp()
        {
            _field = new HeroFakes.EnemyField();
            _squad = new HeroSquad(HeroFakes.LoadConfig(), _field);
        }

        private void Start(params string[] heroCodes) => _squad.Begin(new RunTeam(heroCodes));

        private HeroUnit Hero(int slot) => _squad.Units[slot];

        private void Run(float seconds, float step = 0.05f)
        {
            for (var t = 0f; t < seconds - 1e-4f; t += step) _squad.Tick(step);
        }

        private List<EnemyFoe> Foes()
        {
            var foes = new List<EnemyFoe>();
            _squad.GetFoes(foes);
            return foes;
        }

        // ------------------------------------------------------------------ spawn, fall, respawn

        [Test]
        public void A_new_run_puts_the_team_at_the_castle_edges_by_slot_at_full_health()
        {
            Start("knight", "axeman", "archer");

            var foes = Foes();
            Assert.That(foes, Has.Count.EqualTo(3));
            Assert.That(foes[0].X, Is.EqualTo(-1.5f));
            Assert.That(foes[1].X, Is.EqualTo(1.5f));
            Assert.That(foes[2].X, Is.EqualTo(-1.5f));
            Assert.That(foes[0].HalfWidth, Is.EqualTo(0.5f));
            Assert.That(Hero(0).Health, Is.EqualTo(300f));
            Assert.That(Hero(1).Health, Is.EqualTo(240f));
            Assert.That(Hero(2).Health, Is.EqualTo(120f));
            Assert.That(_squad.Units, Has.All.Matches<HeroUnit>(u => u.Level == 1 && u.IsStanding));
        }

        [Test]
        public void A_hero_hit_to_zero_health_falls_and_enemies_stop_seeing_it()
        {
            Start("knight", "axeman", "archer");

            _squad.Hit(2, 119.5f);
            Assert.That(Foes(), Has.Count.EqualTo(3), "0.5 health left still stands");

            _squad.Hit(2, 0.5f);
            var foes = Foes();
            Assert.That(foes, Has.Count.EqualTo(2));
            Assert.That(foes.TrueForAll(f => f.Key != 2), Is.True);
        }

        [Test]
        public void A_fallen_hero_respawns_after_8s_at_its_spawn_point_at_full_health()
        {
            Start("knight");
            _field.Add(10f);
            Run(2f);                       // walked out towards the enemy
            _squad.Hit(0, 300f);

            Run(7.9f);
            Assert.That(Hero(0).IsStanding, Is.False);

            Run(0.15f);
            Assert.That(Hero(0).IsStanding, Is.True);
            Assert.That(Hero(0).X, Is.EqualTo(-1.5f));
            Assert.That(Hero(0).Health, Is.EqualTo(300f));
        }

        [Test]
        public void A_respawning_hero_cannot_be_hit_or_seen_while_it_grows_back()
        {
            Start("knight");
            _squad.Hit(0, 300f);
            Run(8.05f);
            Assert.That(Hero(0).IsStanding, Is.True);

            _squad.Hit(0, 1000f);
            Assert.That(Foes(), Is.Empty);
            Assert.That(Hero(0).Health, Is.EqualTo(300f));

            Run(0.35f);
            Assert.That(Foes(), Has.Count.EqualTo(1));
        }

        // ------------------------------------------------------------------ level and cheer

        [Test]
        public void Levelling_a_wounded_hero_adds_the_max_health_gained()
        {
            Start("knight");
            _squad.Hit(0, 200f);           // 100 / 300

            _squad.ApplyLevel(0, 2);       // 145 / 345

            Assert.That(Hero(0).Stats.MaxHealth, Is.EqualTo(345f).Within(1e-3f));
            Assert.That(Hero(0).Health, Is.EqualTo(145f).Within(1e-3f));
        }

        [Test]
        public void Level_5_knight_has_the_designed_stats()
        {
            Start("knight");

            _squad.ApplyLevel(0, 5);

            Assert.That(Hero(0).Stats.MaxHealth, Is.EqualTo(480f).Within(1e-3f));
            Assert.That(Hero(0).Stats.Damage, Is.EqualTo(19.2f).Within(1e-3f));
            Assert.That(Hero(0).Stats.SecondsPerAttack, Is.EqualTo(0.893f).Within(1e-3f));
        }

        [Test]
        public void Level_10_archer_has_the_designed_stats_and_range_and_speed_stay()
        {
            Start("archer");

            _squad.ApplyLevel(0, 10);

            Assert.That(Hero(0).Stats.MaxHealth, Is.EqualTo(282f).Within(1e-3f));
            Assert.That(Hero(0).Stats.Damage, Is.EqualTo(32.9f).Within(1e-3f));
            Assert.That(Hero(0).Stats.SecondsPerAttack, Is.EqualTo(0.630f).Within(1e-3f));
            Assert.That(Hero(0).Hero.range, Is.EqualTo(7f));
            Assert.That(Hero(0).Hero.moveSpeed, Is.EqualTo(3f));
        }

        [Test]
        public void Attack_speed_stops_rising_at_level_21_while_health_and_damage_keep_rising()
        {
            Start("knight");

            _squad.ApplyLevel(0, 21);
            var at21 = Hero(0).Stats;
            _squad.ApplyLevel(0, 30);
            var at30 = Hero(0).Stats;

            Assert.That(at21.SecondsPerAttack, Is.EqualTo(1f / 1.6f).Within(1e-4f));
            Assert.That(at30.SecondsPerAttack, Is.EqualTo(at21.SecondsPerAttack).Within(1e-6f));
            Assert.That(at30.MaxHealth, Is.GreaterThan(at21.MaxHealth));
            Assert.That(at30.Damage, Is.GreaterThan(at21.Damage));
        }

        [Test]
        public void Buying_levels_one_by_one_ends_the_same_as_one_jump()
        {
            Start("knight", "axeman");
            _squad.Hit(0, 200f);
            _squad.Hit(1, 140f);

            _squad.ApplyLevel(0, 2);
            _squad.ApplyLevel(0, 3);
            _squad.ApplyLevel(1, 3);

            Assert.That(Hero(0).Stats.MaxHealth, Is.EqualTo(390f).Within(1e-3f));
            Assert.That(Hero(0).Health, Is.EqualTo(190f).Within(1e-3f));
            Assert.That(Hero(1).Health, Is.EqualTo(100f + 72f).Within(1e-3f));
        }

        [Test]
        public void The_level_up_pop_grows_to_1_2_then_returns_within_0_3_seconds()
        {
            var config = HeroFakes.LoadConfig();
            var peak = config.LevelUpPopScale;
            var seconds = config.LevelUpPopSeconds;

            Assert.That(peak, Is.EqualTo(1.2f).Within(1e-4f));
            Assert.That(seconds, Is.EqualTo(0.3f).Within(1e-4f));
            Assert.That(HeroLevelUpPop.ScaleAt(seconds, peak, seconds), Is.EqualTo(1f));
            Assert.That(HeroLevelUpPop.ScaleAt(seconds * 0.5f, peak, seconds), Is.EqualTo(1.2f).Within(1e-4f));
            Assert.That(HeroLevelUpPop.ScaleAt(seconds * 0.25f, peak, seconds), Is.InRange(1f, 1.2f));
            Assert.That(HeroLevelUpPop.ScaleAt(0f, peak, seconds), Is.EqualTo(1f));
        }

        [Test]
        public void A_hero_levelled_while_fallen_respawns_at_the_new_full_health()
        {
            Start("knight");
            _squad.Hit(0, 300f);

            _squad.ApplyLevel(0, 2);
            Run(8.05f);

            Assert.That(Hero(0).IsStanding, Is.True);
            Assert.That(Hero(0).Health, Is.EqualTo(345f).Within(1e-3f));
        }

        [Test]
        public void Cheer_heals_standing_heroes_up_to_their_max_and_not_fallen_ones()
        {
            Start("knight", "axeman");
            _squad.Hit(0, 50f);            // knight 250 / 300
            _squad.Hit(1, 240f);           // axeman falls

            _squad.HealAll(0.5f);

            Assert.That(Hero(0).Health, Is.EqualTo(300f));
            Assert.That(Hero(1).IsStanding, Is.False);
            Assert.That(Hero(1).Health, Is.EqualTo(0f));
        }

        // ------------------------------------------------------------------ targeting and walking

        [Test]
        public void A_hero_walks_towards_an_enemy_at_its_run_speed()
        {
            Start("knight");
            _field.Add(10f);

            Run(1f);

            Assert.That(Hero(0).X, Is.EqualTo(0.5f).Within(1e-3f), "2.0u/s from -1.5");
            Assert.That(Hero(0).Facing, Is.EqualTo(1));
            Assert.That(Hero(0).IsMoving, Is.True);
        }

        [Test]
        public void A_hero_stops_once_the_enemy_is_in_range_and_hits_at_once_then_every_attack_period()
        {
            Start("knight");
            var enemy = _field.Add(10f);

            Run(5f, 0.01f);                // arrives at x = 10 - 0.9 - 1.2 = 7.9 after 4.7s

            Assert.That(Hero(0).X, Is.EqualTo(7.9f).Within(0.01f));
            Assert.That(Hero(0).Mode, Is.EqualTo(HeroMode.Attacking));
            Assert.That(_field.Taken(enemy), Is.EqualTo(12f), "first hit lands on arrival");

            Run(1f, 0.01f);
            Assert.That(_field.Taken(enemy), Is.EqualTo(24f));
        }

        [Test]
        public void A_hero_targets_the_nearest_enemy_on_either_side()
        {
            Start("knight");               // x = -1.5
            _field.Add(-9f);
            _field.Add(5f);

            Run(1f);

            Assert.That(Hero(0).X, Is.EqualTo(0.5f).Within(1e-3f), "went right, to the nearer enemy");
        }

        [Test]
        public void A_hero_ignores_enemies_outside_the_screen()
        {
            Start("knight");
            var enemy = _field.Add(16.5f);

            Run(1f);
            Assert.That(Hero(0).X, Is.EqualTo(-1.5f));

            _field.Move(enemy, 15.9f);
            Run(1f);
            Assert.That(Hero(0).X, Is.EqualTo(0.5f).Within(1e-3f));
        }

        [Test]
        public void A_hitting_hero_keeps_its_target_until_it_is_defeated()
        {
            Start("knight");               // x = -1.5, front edge -1.0
            var first = _field.Add(0f, health: 24f);
            Run(0.05f);
            Assert.That(_field.Taken(first), Is.EqualTo(12f));

            var closer = _field.Add(-1.2f);    // now nearer, but the knight is busy
            Run(1.05f);
            Assert.That(_field.Taken(first), Is.EqualTo(24f));
            Assert.That(_field.Taken(closer), Is.EqualTo(0f));

            Run(1.05f);
            Assert.That(_field.Taken(closer), Is.EqualTo(12f), "picks the next enemy once the first is defeated");
        }

        [Test]
        public void With_no_enemy_left_a_hero_walks_back_to_its_spawn_point()
        {
            Start("knight");
            var enemy = _field.Add(15f);
            Run(2f);                       // x = 2.5
            _field.Remove(enemy);

            Run(1f);
            Assert.That(Hero(0).X, Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(Hero(0).Facing, Is.EqualTo(-1));
            Assert.That(Hero(0).Mode, Is.EqualTo(HeroMode.Waiting));

            Run(3f);
            Assert.That(Hero(0).X, Is.EqualTo(-1.5f));
            Assert.That(Hero(0).IsMoving, Is.False);
        }

        // ------------------------------------------------------------------ attack kinds

        [Test]
        public void The_axeman_cleave_hits_every_enemy_in_the_stretch_in_front_but_not_behind()
        {
            Start("axeman");               // x = -1.5; stretch from -1.5 to 0.5 when facing right
            var target = _field.Add(0f);
            var inStretch = _field.Add(0.9f);   // left edge 0.5 touches the stretch
            var beyond = _field.Add(2f);
            var behind = _field.Add(-4f);

            Run(0.05f);

            Assert.That(_field.Taken(target), Is.EqualTo(14f));
            Assert.That(_field.Taken(inStretch), Is.EqualTo(14f));
            Assert.That(_field.Taken(beyond), Is.EqualTo(0f));
            Assert.That(_field.Taken(behind), Is.EqualTo(0f));
        }

        [Test]
        public void A_single_target_hero_hits_only_its_target()
        {
            Start("knight");
            var target = _field.Add(0f);
            var next = _field.Add(0.9f);

            Run(0.05f);

            Assert.That(_field.Taken(target), Is.EqualTo(12f));
            Assert.That(_field.Taken(next), Is.EqualTo(0f));
        }

        [Test]
        public void The_mage_blast_hits_every_enemy_close_around_the_target()
        {
            Start("mage");                 // x = -1.5, range 6
            var target = _field.Add(3f);
            var near = _field.Add(4.8f);   // edge 1.4u from the target's centre
            var far = _field.Add(5.2f);    // edge 1.8u away

            Run(0.05f);

            Assert.That(_field.Taken(target), Is.EqualTo(18f));
            Assert.That(_field.Taken(near), Is.EqualTo(18f));
            Assert.That(_field.Taken(far), Is.EqualTo(0f));
        }

        [Test]
        public void An_enemy_defeated_earlier_in_the_frame_is_not_hit_again_by_another_hero()
        {
            Start("axeman", "knight");     // axeman x = -1.5, knight x = +1.5
            var weak = _field.Add(0f, health: 10f);

            Run(0.05f);

            Assert.That(_field.Taken(weak), Is.EqualTo(14f), "defeated by the axeman; the knight chose nothing else");
        }

        [Test]
        public void Leaving_the_battle_removes_every_hero()
        {
            Start("knight", "axeman", "archer");

            _squad.Clear();

            Assert.That(_squad.Units, Is.Empty);
            Assert.That(Foes(), Is.Empty);
        }
    }
}
