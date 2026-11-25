using System;
using System.Collections.Generic;
using Game.Run;
using NUnit.Framework;
using UnityEngine;

namespace Game.Castle.Tests
{
    public class CastleSessionTests
    {
        private CastleConfig _config;
        private FakeRunFlow _run;
        private FakeEnemyField _enemies;
        private CastleBody _body;
        private CastleSkills _skills;
        private CastleUpgrades _upgrades;
        private FakeSpawner _spawner;
        private CastleSession _session;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CastleConfig>();
            _run = new FakeRunFlow();
            _enemies = new FakeEnemyField();
            _body = new CastleBody(_config, _run);
            _skills = new CastleSkills(_config, _body, _run, _enemies, new FakeHeroSquad());
            _upgrades = new CastleUpgrades(_body, _skills);
            var autoShot = new CastleAutoShot(_config, _body, _enemies, _upgrades);
            _spawner = new FakeSpawner();
            _session = new CastleSession(_config, _run, _run, _enemies, _body, _skills, _upgrades, autoShot, _spawner);
            _session.Run();
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_config);

        private void StartBattle()
        {
            _run.StartRun();
            _run.StartBattle();
        }

        [Test]
        public void Castle_appears_full_size_when_a_run_starts()
        {
            Assert.That(_spawner.View.Visible, Is.False);
            Assert.That(_spawner.View.Calls[0], Is.EqualTo("place 1.5 4"));

            _run.StartRun();

            Assert.That(_spawner.View.Visible, Is.True);
            Assert.That(_spawner.View.Calls, Does.Contain("reset"));
        }

        [Test]
        public void Castle_shoots_only_once_the_battle_started()
        {
            _enemies.AddEnemy(1, 3f);
            _run.StartRun();
            _spawner.View.Tick(1f);
            Assert.That(_enemies.Damaged, Is.Empty);

            _run.StartBattle();
            _spawner.View.Tick(0.02f);

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(1));
            Assert.That(_spawner.View.Calls, Does.Contain("shot 1"));
        }

        [Test]
        public void Nothing_moves_while_paused()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            _run.IsPaused = true;
            _spawner.View.Calls.Clear();

            _spawner.View.Tick(1f);

            Assert.That(_enemies.Damaged, Is.Empty);
            Assert.That(_spawner.View.Calls, Does.Not.Contain("advance"));
        }

        [Test]
        public void Shot_timer_stands_still_while_paused_and_goes_on_from_there()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            _spawner.View.Tick(0.02f);
            _spawner.View.Tick(0.5f);

            _run.IsPaused = true;
            _spawner.View.Tick(10f);
            _run.IsPaused = false;
            _spawner.View.Tick(0.45f);
            Assert.That(_enemies.Damaged, Has.Count.EqualTo(1), "the paused 10s did not count");

            _spawner.View.Tick(0.06f);
            Assert.That(_enemies.Damaged, Has.Count.EqualTo(2));
        }

        [Test]
        public void Castle_stops_shooting_when_the_battle_stops()
        {
            StartBattle();
            _run.StopBattle();
            _enemies.AddEnemy(1, 3f);

            _spawner.View.Tick(1f);

            Assert.That(_enemies.Damaged, Is.Empty);
        }

        [Test]
        public void Enemy_hit_flashes_the_castle_and_its_fall_collapses_it()
        {
            StartBattle();

            _body.Hit(0, 10f);
            _body.Hit(0, 5000f);

            Assert.That(_spawner.View.Calls, Does.Contain("flash"));
            Assert.That(_spawner.View.Calls, Does.Contain("collapse"));
        }

        [Test]
        public void Thunder_shows_its_effect_on_every_enemy_struck()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            _enemies.AddEnemy(2, -4f);

            _skills.TryUse(CastleSkill.Thunder);

            Assert.That(_spawner.View.Calls, Does.Contain("thunder 3"));
            Assert.That(_spawner.View.Calls, Does.Contain("thunder -4"));
        }

        [Test]
        public void Skill_cooldown_runs_down_during_the_battle()
        {
            StartBattle();
            _skills.TryUse(CastleSkill.Thunder);

            _spawner.View.Tick(5f);

            Assert.That(_skills.GetState(CastleSkill.Thunder).CooldownLeft, Is.EqualTo(15f).Within(1e-4f));
        }

        [Test]
        public void Leaving_the_battle_hides_the_castle_and_its_effects()
        {
            StartBattle();
            _run.ClearBattle();

            Assert.That(_spawner.View.Visible, Is.False);
            Assert.That(_spawner.View.Calls, Does.Contain("clear"));
        }

        [Test]
        public void New_run_rebuilds_a_fallen_castle()
        {
            StartBattle();
            _body.Hit(0, 5000f);

            _run.StartRun();

            Assert.That(_body.IsDestroyed, Is.False);
            Assert.That(_body.Health, Is.EqualTo(1000f));
        }

        [Test]
        public void Buying_a_castle_level_pops_the_castle_for_a_moment()
        {
            StartBattle();

            _upgrades.ApplyLevel(CastleUpgrade.Attack, 2);

            Assert.That(_spawner.View.Calls, Does.Contain("pop 1.1 0.3"));
        }

        [Test]
        public void Fallen_castle_does_not_pop_on_a_level()
        {
            StartBattle();
            _body.Hit(0, 5000f);
            _spawner.View.Calls.Clear();

            _upgrades.ApplyLevel(CastleUpgrade.Health, 2);

            Assert.That(_spawner.View.Calls, Has.None.StartsWith("pop"));
        }

        [Test]
        public void Hit_flash_lasts_a_tenth_of_a_second_and_the_fall_shrinks_over_half_a_second()
        {
            StartBattle();

            _body.Hit(0, 10f);
            _body.Hit(0, 5000f);

            Assert.That(_spawner.View.FlashSeconds, Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(_spawner.View.CollapseSeconds, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void Shot_drawing_flies_for_fifteen_hundredths_of_a_second_after_the_damage_was_dealt()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);

            _spawner.View.Tick(0.02f);

            Assert.That(_enemies.Damaged, Is.EqualTo(new[] { (1, 10f) }), "damage is dealt when the shot fires");
            Assert.That(_spawner.View.ShotSeconds, Is.EqualTo(0.15f).Within(1e-5f));
        }

        [Test]
        public void Shot_drawing_vanishes_when_its_target_is_defeated_by_someone_else()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            _spawner.View.Tick(0.02f);
            var damageBefore = _enemies.Damaged.Count;

            _enemies.Defeat(1);

            Assert.That(_spawner.View.Calls, Does.Contain("drop 1"));
            Assert.That(_enemies.Damaged, Has.Count.EqualTo(damageBefore), "no damage moves to another enemy");
        }

        [Test]
        public void Thunder_effect_lasts_three_tenths_of_a_second()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);

            _skills.TryUse(CastleSkill.Thunder);

            Assert.That(_spawner.View.ThunderSeconds, Is.EqualTo(0.3f).Within(1e-5f));
        }

        [Test]
        public void Castle_keeps_shooting_through_the_rest_between_two_waves()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            _spawner.View.Tick(0.02f);

            _spawner.View.Tick(3f); // the 3s rest between waves: no battle signal arrives

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(2));
        }

        [Test]
        public void Skill_cooldown_keeps_running_into_the_next_wave()
        {
            StartBattle();
            _skills.TryUse(CastleSkill.Thunder);

            _spawner.View.Tick(3f);
            _spawner.View.Tick(10f);

            Assert.That(_skills.GetState(CastleSkill.Thunder).CooldownLeft, Is.EqualTo(7f).Within(1e-4f));
        }

        [Test]
        public void New_run_puts_every_castle_level_back_to_one()
        {
            StartBattle();
            _upgrades.ApplyLevel(CastleUpgrade.Health, 4);
            _upgrades.ApplyLevel(CastleUpgrade.Attack, 4);
            _upgrades.ApplyLevel(CastleUpgrade.Skills, 4);

            _run.StartRun();

            Assert.That(_body.MaxHealth, Is.EqualTo(1000f));
            Assert.That(_upgrades.AttackLevel, Is.EqualTo(1));
            _upgrades.ApplyLevel(CastleUpgrade.Health, 2);
            Assert.That(_body.MaxHealth, Is.EqualTo(1150f), "level 2 counts again after the reset");
        }

        [Test]
        public void Run_revive_stands_the_castle_up_at_thirty_percent_and_grows_it_back_over_half_a_second()
        {
            StartBattle();
            _body.Hit(0, 5000f);

            _run.StartRevive();

            Assert.That(_body.IsDestroyed, Is.False);
            Assert.That(_body.Health, Is.EqualTo(300f));
            Assert.That(_spawner.View.Calls, Does.Contain("grow"));
            Assert.That(_spawner.View.GrowBackSeconds, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void Run_is_told_the_revive_is_shown_once_the_castle_has_grown_back()
        {
            StartBattle();
            _body.Hit(0, 5000f);
            _run.StartRevive();
            _spawner.View.Calls.Clear();

            _spawner.View.Tick(0.3f);
            Assert.That(_run.ReviveShownCalls, Is.EqualTo(0), "still growing although the battle stands still");
            Assert.That(_spawner.View.Calls, Does.Contain("advance"));

            _spawner.View.Tick(0.25f);
            _spawner.View.Tick(0.5f);
            Assert.That(_run.ReviveShownCalls, Is.EqualTo(1));
        }

        [Test]
        public void While_reviving_the_castle_does_not_shoot_and_cooldowns_stand_still()
        {
            StartBattle();
            _skills.TryUse(CastleSkill.Thunder);
            _body.Hit(0, 5000f);
            _enemies.AddEnemy(1, 3f);
            var damageBefore = _enemies.Damaged.Count;
            _run.StartRevive();

            _spawner.View.Tick(0.4f);

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(damageBefore));
            Assert.That(_skills.GetState(CastleSkill.Thunder).CooldownLeft, Is.EqualTo(20f).Within(1e-4f));
        }

        [Test]
        public void Revived_castle_shoots_as_soon_as_the_battle_runs_again()
        {
            StartBattle();
            _body.Hit(0, 5000f);
            _enemies.AddEnemy(1, 3f);
            _run.StartRevive();
            _spawner.View.Tick(0.6f);

            _run.Step = RunStep.Playing;
            _run.IsPaused = false;
            _spawner.View.Tick(0.02f);

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(1));
        }

        [Test]
        public void Fall_keeps_shrinking_while_the_run_waits_to_offer_a_revive_but_not_in_the_pause_popup()
        {
            StartBattle();
            _body.Hit(0, 5000f);
            _run.Step = RunStep.ReviveWait;
            _run.IsPaused = true;
            _spawner.View.Calls.Clear();

            _spawner.View.Tick(0.1f);
            Assert.That(_spawner.View.Calls, Does.Contain("advance"));

            _run.Step = RunStep.Paused;
            _spawner.View.Calls.Clear();
            _spawner.View.Tick(0.1f);
            Assert.That(_spawner.View.Calls, Does.Not.Contain("advance"));
        }

        [Test]
        public void Ending_the_session_removes_the_view()
        {
            _session.DisposeAsync();

            Assert.That(_spawner.Despawned, Is.True);
        }
    }
}
