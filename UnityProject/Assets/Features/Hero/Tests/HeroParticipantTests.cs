using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Hero.Tests
{
    /// <summary>
    /// What the player sees of the team through the run: shown at run start, moved on by the shared clock's steps
    /// (0.025 s each), gone on clear.
    /// </summary>
    public sealed class HeroParticipantTests
    {
        private HeroFakes.RunFlow _run;
        private HeroFakes.EnemyField _field;
        private HeroFakes.DisplayFactory _displays;
        private HeroFakes.Combat _combat;
        private HeroSquad _squad;
        private HeroParticipant _participant;

        [SetUp]
        public void SetUp()
        {
            var config = HeroFakes.LoadConfig();
            _run = new HeroFakes.RunFlow();
            _field = new HeroFakes.EnemyField();
            _displays = new HeroFakes.DisplayFactory();
            _combat = new HeroFakes.Combat();
            _squad = new HeroSquad(config, _field);
            var presenter = new HeroPresenter(_squad, config, _displays, new HeroFakes.Art());
            _participant = new HeroParticipant(_run, _squad, presenter, _combat, new HeroFakes.StillClock());
            _participant.Run();
        }

        [TearDown]
        public void TearDown() => _participant.DisposeAsync();

        /// <summary>A frame of <paramref name="seconds"/>: the clock runs its steps, then the view redraws.</summary>
        private void Play(float seconds)
        {
            _combat.Run(seconds);
            _participant.RefreshView(seconds);
        }

        /// <summary>A full team whose slots 2 and 3 fall at once, so the test watches the hero of slot 1 alone.</summary>
        private void StartWithOnlySlot1Standing(string slot1 = "knight")
        {
            _run.StartRun(slot1, slot1 == "axeman" ? "knight" : "axeman", "archer");
            _squad.Hit(1, 1000f);
            _squad.Hit(2, 1000f);
        }

        [Test]
        public void Heroes_move_and_hit_as_a_combat_actor_once_per_combat_tick()
        {
            Assert.That(_combat.Actors, Is.EqualTo(new Game.Combat.ICombatActor[] { _participant }));
        }

        [Test]
        public void Heroes_aim_from_the_start_of_the_tick_and_hit_after_every_actor_aimed()
        {
            StartWithOnlySlot1Standing("knight");
            var enemy = _field.Add(0f);

            _participant.Aim(HeroFakes.Combat.Step);
            Assert.That(_field.Taken(enemy), Is.EqualTo(0f), "aiming does not hit");
            Assert.That(_squad.Units[0].TargetHandle, Is.EqualTo(enemy));

            _participant.Strike(HeroFakes.Combat.Step);
            Assert.That(_field.Taken(enemy), Is.EqualTo(12f).Within(1e-4f), "the hit lands in strike");
        }

        [Test]
        public void A_knight_hits_at_once_then_again_exactly_40_steps_later()
        {
            StartWithOnlySlot1Standing("knight");
            var enemy = _field.Add(0f);

            _combat.RunSteps(1);
            Assert.That(_field.Taken(enemy), Is.EqualTo(12f).Within(1e-4f), "first hit on the first step");

            _combat.RunSteps(39);
            Assert.That(_field.Taken(enemy), Is.EqualTo(12f).Within(1e-4f), "1.0 s not over after 39 steps");

            _combat.RunSteps(1);
            Assert.That(_field.Taken(enemy), Is.EqualTo(24f).Within(1e-4f), "second hit on step 40 after the first");
        }

        [Test]
        public void A_target_slowed_by_frost_is_kept_and_hit_at_the_same_rhythm()
        {
            StartWithOnlySlot1Standing("knight");
            var enemy = _field.Add(0f);
            _combat.RunSteps(1);                    // first hit

            _field.SlowAll(5f);                    // the castle's frost: only the enemies' own speed changes
            _combat.RunSteps(40);

            Assert.That(_squad.Units[0].TargetHandle, Is.EqualTo(enemy), "the hero keeps its target");
            Assert.That(_squad.Units[0].Mode, Is.EqualTo(HeroMode.Attacking));
            Assert.That(_field.Taken(enemy), Is.EqualTo(24f).Within(1e-4f), "the next hit still lands 1.0 s later");
        }

        [Test]
        public void A_swordsman_waits_exactly_28_steps_between_hits()
        {
            StartWithOnlySlot1Standing("swordsman");
            var enemy = _field.Add(0f);

            _combat.RunSteps(1);
            _combat.RunSteps(27);
            Assert.That(_field.Taken(enemy), Is.EqualTo(16f).Within(1e-4f), "0.7 s not over after 27 steps");

            _combat.RunSteps(1);
            Assert.That(_field.Taken(enemy), Is.EqualTo(32f).Within(1e-4f), "0.7 s wait ends after 28 steps");
        }

        [Test]
        public void A_fallen_hero_respawns_on_step_320_after_it_fell()
        {
            StartWithOnlySlot1Standing();
            _squad.Hit(0, 1000f);

            _combat.RunSteps(319);
            Assert.That(_squad.Units[0].IsStanding, Is.False, "8 s not over after 319 steps");

            _combat.RunSteps(1);
            Assert.That(_squad.Units[0].IsStanding, Is.True, "8 s wait ends after 320 steps");
        }

        [Test]
        public void Steps_the_run_does_not_let_play_leave_heroes_where_they_are()
        {
            StartWithOnlySlot1Standing();
            var enemy = _field.Add(10f);
            var near = _field.Add(0f);
            _combat.Gameplay = false;

            Play(1f);

            Assert.That(_squad.Units[0].X, Is.EqualTo(-1.5f));
            Assert.That(_field.Taken(near), Is.EqualTo(0f));
            Assert.That(_field.Taken(enemy), Is.EqualTo(0f));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        public void A_team_without_exactly_three_heroes_does_not_enter_the_battle(int heroCount)
        {
            var codes = new string[heroCount];
            System.Array.Copy(new[] { "knight", "axeman", "archer", "mage" }, codes, heroCount);
            var enemy = _field.Add(0f);
            LogAssert.Expect(LogType.Error, new Regex(@"\[Hero\] The run brought \d+ heroes; a team needs 3"));

            _run.StartRun(codes);
            Play(1f);

            Assert.That(_displays.Made, Is.Empty);
            Assert.That(_squad.Units, Is.Empty);
            Assert.That(_field.Taken(enemy), Is.EqualTo(0f));
        }

        [Test]
        public void A_short_team_on_replay_takes_the_previous_team_off_the_field()
        {
            _run.StartRun("knight", "axeman", "archer");
            LogAssert.Expect(LogType.Error, new Regex(@"\[Hero\] The run brought 2 heroes"));

            _run.StartRun("knight", "axeman");

            Assert.That(_displays.Made, Has.Count.EqualTo(3));
            Assert.That(_displays.Made.TrueForAll(d => d.Removed), Is.True);
            Assert.That(_squad.Units, Is.Empty);
        }

        [Test]
        public void A_new_run_shows_each_hero_of_the_team_at_its_spawn_point()
        {
            _run.StartRun("knight", "axeman", "archer");

            Assert.That(_displays.Made, Has.Count.EqualTo(3));
            Assert.That(_displays.Made[0].HeroCode, Is.EqualTo("knight"));
            Assert.That(_displays.Made[1].HeroCode, Is.EqualTo("axeman"));
            Assert.That(_displays.Made[2].HeroCode, Is.EqualTo("archer"));
            Assert.That(_displays.Made[0].X, Is.EqualTo(-1.5f));
            Assert.That(_displays.Made[1].X, Is.EqualTo(1.5f));
        }

        [Test]
        public void Heroes_on_screen_follow_the_battle_each_frame()
        {
            StartWithOnlySlot1Standing();
            _field.Add(10f);

            Play(1f);

            Assert.That(_displays.Made[0].X, Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(_displays.Made[0].Moving, Is.True);
        }

        [Test]
        public void Once_the_battle_stopped_a_fallen_hero_does_not_respawn()
        {
            StartWithOnlySlot1Standing();
            _squad.Hit(0, 1000f);
            _run.StopBattle();                     // the castle fell the same frame

            for (var i = 0; i < 10; i++) Play(1f);

            Assert.That(_squad.Units[0].IsStanding, Is.False);
            Assert.That(_displays.Made[0].Respawns, Is.EqualTo(0));
        }

        [Test]
        public void An_enemy_left_over_between_waves_is_still_fought()
        {
            StartWithOnlySlot1Standing();
            var leftover = _field.Add(0f);         // no wave signal reaches the heroes; any living enemy counts

            Play(0.05f);

            Assert.That(_field.Taken(leftover), Is.EqualTo(12f));
        }

        [Test]
        public void Once_the_battle_stopped_heroes_no_longer_act()
        {
            StartWithOnlySlot1Standing();
            var enemy = _field.Add(0f);
            _run.StopBattle();

            Play(1f);

            Assert.That(_field.Taken(enemy), Is.EqualTo(0f));
        }

        [Test]
        public void A_hit_shows_a_strike_and_the_enemy_side_sees_hurt_and_fall()
        {
            StartWithOnlySlot1Standing();
            _field.Add(0f);

            Play(0.05f);
            _squad.Hit(0, 10f);
            _squad.Hit(0, 1000f);

            var display = _displays.Made[0];
            Assert.That(display.Strikes, Is.EqualTo(1));
            Assert.That(display.Hurts, Is.EqualTo(1));
            Assert.That(display.Falls, Is.EqualTo(1));
        }

        [Test]
        public void A_fallen_hero_reappears_with_the_respawn_animation()
        {
            StartWithOnlySlot1Standing();
            _squad.Hit(0, 1000f);

            for (var i = 0; i < 81; i++) Play(0.1f);

            Assert.That(_displays.Made[0].Respawns, Is.EqualTo(1));
            Assert.That(_displays.Made[0].X, Is.EqualTo(-1.5f));
        }

        [Test]
        public void Buying_a_level_shows_the_level_up_pop()
        {
            StartWithOnlySlot1Standing();

            _squad.ApplyLevel(0, 2);

            Assert.That(_displays.Made[0].LevelUps, Is.EqualTo(1));
            Assert.That(_displays.Made[0].PopScale, Is.EqualTo(1.2f).Within(1e-4f));
            Assert.That(_displays.Made[0].PopSeconds, Is.EqualTo(0.3f).Within(1e-4f));
        }

        /// <summary>feature-hero/hero-stats/hero-level-values#stage-prototype: the new level number shows for 1 s.</summary>
        [Test]
        public void Buying_a_level_shows_the_new_level_number_above_the_hero_for_1_second()
        {
            StartWithOnlySlot1Standing();

            _squad.ApplyLevel(0, 2);
            Assert.That(_displays.Made[0].ShownLevel, Is.EqualTo(2));
            Assert.That(_displays.Made[0].ShownLevelSeconds, Is.EqualTo(1f).Within(1e-4f));

            _squad.ApplyLevel(0, 3);
            Assert.That(_displays.Made[0].ShownLevel, Is.EqualTo(3), "a new level replaces the number shown");
            Assert.That(_displays.Made[0].LevelLabels, Is.EqualTo(2));
        }

        /// <summary>feature-hero/hero-respawn/respawn-values#stage-prototype: the wait left is shown through the HUD.</summary>
        [Test]
        public void The_respawn_wait_left_counts_down_for_the_hud()
        {
            StartWithOnlySlot1Standing();
            Assert.That(_squad.RespawnSecondsLeft(0), Is.EqualTo(0f), "standing: nothing to wait for");

            _squad.Hit(0, 1000f);
            Assert.That(_squad.RespawnSecondsLeft(0), Is.EqualTo(8f).Within(1e-4f));

            _combat.RunSteps(100);
            Assert.That(_squad.RespawnSecondsLeft(0), Is.EqualTo(5.5f).Within(1e-3f));

            _combat.RunSteps(220);
            Assert.That(_squad.RespawnSecondsLeft(0), Is.EqualTo(0f), "back on its feet");

            _combat.RunSteps(13);                   // 0.3 s growing back (12 steps, +1): no hit lands until it is over
            _squad.Hit(0, 1000f);
            Assert.That(_squad.RespawnSecondsLeft(0), Is.GreaterThan(0f), "down again right before the clear");

            _run.ClearBattle();
            Assert.That(_squad.RespawnSecondsLeft(0), Is.EqualTo(0f), "no run going");
        }

        [Test]
        public void Buying_two_levels_in_one_frame_pops_once_per_level()
        {
            StartWithOnlySlot1Standing();

            _squad.ApplyLevel(0, 2);
            _squad.ApplyLevel(0, 3);

            Assert.That(_displays.Made[0].LevelUps, Is.EqualTo(2));
        }

        [Test]
        public void Leaving_the_battle_removes_the_heroes_from_screen()
        {
            _run.StartRun("knight", "axeman", "archer");

            _run.ClearBattle();

            Assert.That(_displays.Made.TrueForAll(d => d.Removed), Is.True);
        }

        [Test]
        public void A_replay_replaces_the_team_on_screen()
        {
            _run.StartRun("knight", "axeman", "archer");

            _run.StartRun("swordsman", "archer", "mage");

            Assert.That(_displays.Made, Has.Count.EqualTo(6));
            Assert.That(_displays.Made.GetRange(0, 3).TrueForAll(d => d.Removed), Is.True);
            Assert.That(_displays.Made[3].HeroCode, Is.EqualTo("swordsman"));
        }

        [Test]
        public void After_the_session_ends_the_heroes_are_gone_and_runs_are_ignored()
        {
            _run.StartRun("knight", "axeman", "archer");

            _participant.DisposeAsync();
            _run.StartRun("knight", "axeman", "archer");

            Assert.That(_displays.Made, Has.Count.EqualTo(3));
            Assert.That(_displays.Made.TrueForAll(d => d.Removed), Is.True);
            Assert.DoesNotThrow(() => _combat.RunSteps(5), "combat may still tick the ended heroes: they do nothing");
            Assert.That(_squad.Units, Is.Empty);
        }
    }
}
