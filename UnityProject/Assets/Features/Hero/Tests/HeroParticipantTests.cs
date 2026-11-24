using NUnit.Framework;

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
            _run.StartRun("knight");
            _field.Add(10f);

            _participant.Step(1f);

            Assert.That(_displays.Made[0].X, Is.EqualTo(1.0f).Within(1e-3f));
            Assert.That(_displays.Made[0].Moving, Is.True);
        }

        [Test]
        public void While_paused_heroes_and_their_animations_stand_still()
        {
            _run.StartRun("knight");
            _field.Add(10f);
            _run.IsPaused = true;

            _participant.Step(1f);

            Assert.That(_displays.Made[0].X, Is.EqualTo(-1.5f));
            Assert.That(_displays.Made[0].Animated, Is.EqualTo(0f));
        }

        [Test]
        public void Once_the_battle_stopped_heroes_no_longer_act()
        {
            _run.StartRun("knight");
            var enemy = _field.Add(0f);
            _run.StopBattle();

            _participant.Step(1f);

            Assert.That(_field.Taken(enemy), Is.EqualTo(0f));
        }

        [Test]
        public void A_hit_shows_a_strike_and_the_enemy_side_sees_hurt_and_fall()
        {
            _run.StartRun("knight");
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
            _run.StartRun("knight");
            _squad.Hit(0, 1000f);

            for (var i = 0; i < 81; i++) _participant.Step(0.1f);

            Assert.That(_displays.Made[0].Respawns, Is.EqualTo(1));
            Assert.That(_displays.Made[0].X, Is.EqualTo(-1.5f));
        }

        [Test]
        public void Buying_a_level_shows_the_level_up_pop()
        {
            _run.StartRun("knight");

            _squad.ApplyLevel(0, 2);

            Assert.That(_displays.Made[0].LevelUps, Is.EqualTo(1));
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
            _run.StartRun("knight");

            _participant.DisposeAsync();
            _run.StartRun("knight");

            Assert.That(_displays.Made, Has.Count.EqualTo(1));
            Assert.That(_displays.Made[0].Removed, Is.True);
        }
    }
}
