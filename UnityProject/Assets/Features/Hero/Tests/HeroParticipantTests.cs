using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Hero.Tests
{
    /// <summary>What the player sees of the team through the run: shown at run start, frozen on pause, gone on clear.</summary>
    public sealed class HeroParticipantTests
    {
        private HeroFakes.RunFlow _run;
        private HeroFakes.EnemyField _field;
        private HeroFakes.DisplayFactory _displays;
        private HeroSquad _squad;
        private HeroParticipant _participant;

        [SetUp]
        public void SetUp()
        {
            var config = HeroFakes.LoadConfig();
            _run = new HeroFakes.RunFlow();
            _field = new HeroFakes.EnemyField();
            _displays = new HeroFakes.DisplayFactory();
            _squad = new HeroSquad(config, _field);
            var presenter = new HeroPresenter(_squad, config, _displays);
            _participant = new HeroParticipant(_run, _squad, presenter, new HeroFakes.StillClock());
            _participant.Run();
        }

        [TearDown]
        public void TearDown() => _participant.DisposeAsync();

        /// <summary>A full team whose slots 2 and 3 fall at once, so the test watches the knight (slot 1) alone.</summary>
        private void StartWithOnlyTheKnightStanding()
        {
            _run.StartRun("knight", "axeman", "archer");
            _squad.Hit(1, 1000f);
            _squad.Hit(2, 1000f);
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
            _participant.Step(1f);

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
            StartWithOnlyTheKnightStanding();
            _field.Add(10f);

            _participant.Step(1f);

            Assert.That(_displays.Made[0].X, Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(_displays.Made[0].Moving, Is.True);
        }

        [Test]
        public void While_paused_heroes_and_their_animations_stand_still()
        {
            StartWithOnlyTheKnightStanding();
            _field.Add(10f);
            _run.IsPaused = true;

            _participant.Step(1f);

            Assert.That(_displays.Made[0].X, Is.EqualTo(-1.5f));
            Assert.That(_displays.Made[0].Animated, Is.EqualTo(0f));
        }

        [Test]
        public void While_paused_a_hitting_hero_keeps_its_target_and_its_wait()
        {
            StartWithOnlyTheKnightStanding();
            var enemy = _field.Add(0f);
            _participant.Step(0.05f);              // first hit, then 1.0s to wait
            _run.IsPaused = true;

            for (var i = 0; i < 10; i++) _participant.Step(0.5f);

            var hero = _squad.Units[0];
            Assert.That(_field.Taken(enemy), Is.EqualTo(12f));
            Assert.That(hero.TargetHandle, Is.EqualTo(enemy));
            Assert.That(hero.Mode, Is.EqualTo(HeroMode.Attacking));
            Assert.That(hero.Cooldown, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void While_paused_the_respawn_wait_stands_still()
        {
            StartWithOnlyTheKnightStanding();
            _squad.Hit(0, 1000f);
            _run.IsPaused = true;

            for (var i = 0; i < 20; i++) _participant.Step(1f);

            Assert.That(_squad.Units[0].IsStanding, Is.False);
            Assert.That(_squad.Units[0].RespawnLeft, Is.EqualTo(8f).Within(1e-4f));

            _run.IsPaused = false;
            _participant.Step(8.05f);
            Assert.That(_squad.Units[0].IsStanding, Is.True);
        }

        [Test]
        public void Once_the_battle_stopped_a_fallen_hero_does_not_respawn()
        {
            StartWithOnlyTheKnightStanding();
            _squad.Hit(0, 1000f);
            _run.StopBattle();                     // the castle fell the same frame

            for (var i = 0; i < 10; i++) _participant.Step(1f);

            Assert.That(_squad.Units[0].IsStanding, Is.False);
            Assert.That(_displays.Made[0].Respawns, Is.EqualTo(0));
        }

        [Test]
        public void An_enemy_left_over_between_waves_is_still_fought()
        {
            StartWithOnlyTheKnightStanding();
            var leftover = _field.Add(0f);         // no wave signal reaches the heroes; any living enemy counts

            _participant.Step(0.05f);

            Assert.That(_field.Taken(leftover), Is.EqualTo(12f));
        }

        [Test]
        public void Once_the_battle_stopped_heroes_no_longer_act()
        {
            StartWithOnlyTheKnightStanding();
            var enemy = _field.Add(0f);
            _run.StopBattle();

            _participant.Step(1f);

            Assert.That(_field.Taken(enemy), Is.EqualTo(0f));
        }

        [Test]
        public void A_hit_shows_a_strike_and_the_enemy_side_sees_hurt_and_fall()
        {
            StartWithOnlyTheKnightStanding();
            _field.Add(0f);

            _participant.Step(0.05f);
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
            StartWithOnlyTheKnightStanding();
            _squad.Hit(0, 1000f);

            for (var i = 0; i < 81; i++) _participant.Step(0.1f);

            Assert.That(_displays.Made[0].Respawns, Is.EqualTo(1));
            Assert.That(_displays.Made[0].X, Is.EqualTo(-1.5f));
        }

        [Test]
        public void Buying_a_level_shows_the_level_up_pop()
        {
            StartWithOnlyTheKnightStanding();

            _squad.ApplyLevel(0, 2);

            Assert.That(_displays.Made[0].LevelUps, Is.EqualTo(1));
            Assert.That(_displays.Made[0].PopScale, Is.EqualTo(1.2f).Within(1e-4f));
            Assert.That(_displays.Made[0].PopSeconds, Is.EqualTo(0.3f).Within(1e-4f));
        }

        [Test]
        public void Buying_two_levels_in_one_frame_pops_once_per_level()
        {
            StartWithOnlyTheKnightStanding();

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
        }
    }
}
