using System;
using Game.Run;
using NUnit.Framework;

namespace Game.Hero.Tests
{
    /// <summary>The castle gives where each slot starts and respawns; heroes use it from the next run start.</summary>
    public sealed class HeroSpawnPointsTests
    {
        private HeroFakes.EnemyField _field;
        private HeroSquad _squad;

        [SetUp]
        public void SetUp()
        {
            _field = new HeroFakes.EnemyField();
            _squad = new HeroSquad(HeroFakes.LoadConfig(), _field);
        }

        private void StartRun() => _squad.Begin(new RunTeam(new[] { "knight", "axeman", "archer" }));

        [Test]
        public void A_run_starts_each_hero_at_the_point_the_castle_gave_for_its_slot()
        {
            _squad.SetSpawnX(new[] { -2f, 2.5f, -3f });

            StartRun();

            Assert.That(_squad.Units[0].X, Is.EqualTo(-2f));
            Assert.That(_squad.Units[1].X, Is.EqualTo(2.5f));
            Assert.That(_squad.Units[2].X, Is.EqualTo(-3f));
        }

        [Test]
        public void A_fallen_hero_respawns_at_the_point_the_castle_gave_for_its_slot()
        {
            _squad.SetSpawnX(new[] { -2f, 2.5f, -3f });
            StartRun();
            _squad.Hit(1, 1000f);

            for (var i = 0; i < 81; i++) _squad.Tick(0.1f);

            Assert.That(_squad.Units[1].IsStanding, Is.True);
            Assert.That(_squad.Units[1].X, Is.EqualTo(2.5f));
        }

        [Test]
        public void New_points_wait_for_the_next_run_and_heroes_on_the_field_keep_theirs()
        {
            StartRun();
            _squad.SetSpawnX(new[] { -2f, 2.5f, -3f });
            _squad.Hit(0, 1000f);

            for (var i = 0; i < 81; i++) _squad.Tick(0.1f);

            Assert.That(_squad.Units[0].X, Is.EqualTo(-1.5f), "respawns at the point of the run it is in");

            StartRun();
            Assert.That(_squad.Units[0].X, Is.EqualTo(-2f));
        }

        [Test]
        public void Without_points_from_the_castle_heroes_start_at_the_config_points()
        {
            StartRun();

            Assert.That(_squad.Units[0].X, Is.EqualTo(-1.5f));
            Assert.That(_squad.Units[1].X, Is.EqualTo(1.5f));
            Assert.That(_squad.Units[2].X, Is.EqualTo(-1.5f));
        }

        [Test]
        public void A_list_missing_a_slot_is_refused_naming_the_slot_and_the_old_points_stay()
        {
            var error = Assert.Throws<ArgumentException>(() => _squad.SetSpawnX(new[] { -2f, 2.5f }));

            Assert.That(error.Message, Does.Contain("slot 3"));
            StartRun();
            Assert.That(_squad.Units[2].X, Is.EqualTo(-1.5f));
        }

        [Test]
        public void An_empty_list_is_refused_naming_slot_1()
        {
            var error = Assert.Throws<ArgumentException>(() => _squad.SetSpawnX(Array.Empty<float>()));

            Assert.That(error.Message, Does.Contain("slot 1"));
        }

        [Test]
        public void The_castle_giving_its_edges_puts_the_team_where_the_design_says()
        {
            _squad.SetSpawnX(new[] { -1.5f, 1.5f, -1.5f });

            StartRun();

            Assert.That(_squad.Units[0].X, Is.EqualTo(-1.5f));
            Assert.That(_squad.Units[1].X, Is.EqualTo(1.5f));
            Assert.That(_squad.Units[2].X, Is.EqualTo(-1.5f));
        }
    }
}
