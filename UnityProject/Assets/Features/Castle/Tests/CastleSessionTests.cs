using System;
using System.Collections.Generic;
using Game.Core;
using Game.Run;
using NUnit.Framework;
using UnityEngine;

namespace Game.Castle.Tests
{
    /// <summary>
    /// The castle in the scene session. Gameplay (auto shot, cooldowns) steps on the shared clock (<see cref="FakeSimClock"/>);
    /// drawing runs on frame time (<see cref="FakeView.Tick"/>).
    /// </summary>
    public class CastleSessionTests
    {
        private CastleConfig _config;
        private FakeRunFlow _run;
        private FakeEnemyField _enemies;
        private CastleBody _body;
        private CastleSkills _skills;
        private CastleUpgrades _upgrades;
        private FakeSpawner _spawner;
        private FakeSimClock _clock;
        private CastleSession _session;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CastleConfig>();
            _run = new FakeRunFlow();
            _enemies = new FakeEnemyField();
            _body = new CastleBody(_config, _run);
            _skills = new CastleSkills(_config, _body, _run, _enemies, new FakeHeroSquad(), _enemies);
            _upgrades = new CastleUpgrades(_body, _skills);
            var autoShot = new CastleAutoShot(_config, _body, _enemies, _upgrades, _enemies);
            _spawner = new FakeSpawner();
            _clock = new FakeSimClock();
            _session = new CastleSession(_config, _run, _run, _enemies, _body, _skills, _upgrades, autoShot, _spawner, _clock);
            _session.Run();
        }

        [TearDown]
        public void TearDown()
        {
            _spawner.View.DestroyObject();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private ISessionParticipantUsage Usage => _session;

        // Session usage tracking (proposal 2026-11-25-session-usage-tracking).

        [Test]
        public void Castle_does_not_ask_the_session_to_check_for_a_view_right_after_the_run_step()
        {
            Assert.That(Usage.OpensViewsOnRun, Is.False, "the game opens on the home screen; the castle shows once a battle is set up");
        }

        [Test]
        public void Castle_view_counts_as_held_while_the_session_runs_and_not_after_it_ends()
        {
            Assert.That(Usage.HeldViews, Is.EqualTo(1));

            _session.DisposeAsync();

            Assert.That(Usage.HeldViews, Is.EqualTo(0));
        }

        [Test]
        public void Clock_loop_cooldowns_and_revive_wait_count_as_jobs_and_all_end_with_the_session()
        {
            Assert.That(Usage.RunningJobs, Is.EqualTo(1), "the clock loop");

            StartBattle();
            _skills.TryUse(CastleSkill.Thunder);
            _clock.Steps(1);
            Assert.That(Usage.RunningJobs, Is.EqualTo(2), "a cooldown runs down");

            _body.Hit(0, 5000f);
            _run.StartRevive();
            Assert.That(Usage.RunningJobs, Is.EqualTo(3), "the revive grow-back wait");

            _session.DisposeAsync();

            Assert.That(Usage.RunningJobs, Is.EqualTo(0));
            Assert.That(Usage.HeldViews, Is.EqualTo(0));
        }

        [Test]
        public void Cooldown_job_ends_when_every_cooldown_has_run_down()
        {
            StartBattle();
            _skills.TryUse(CastleSkill.Rally); // 25 s
            _clock.Steps(1);
            Assert.That(Usage.RunningJobs, Is.EqualTo(2));

            _clock.Run(25f);

            Assert.That(Usage.RunningJobs, Is.EqualTo(1));
        }

        [Test]
        public void Revive_wait_job_ends_once_the_castle_has_grown_back()
        {
            StartBattle();
            _body.Hit(0, 5000f);
            _run.StartRevive();

            _spawner.View.Tick(0.6f);

            Assert.That(Usage.RunningJobs, Is.EqualTo(1));
        }

        // PROTOTYPE body feedback (castle-body-values, Thông số theo giai đoạn).

        [Test]
        public void Hit_flashes_and_shakes_the_castle_lightly_for_a_tenth_of_a_second()
        {
            StartBattle();

            _body.Hit(0, 10f);

            Assert.That(_spawner.View.FlashSeconds, Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(_spawner.View.ShakeSeconds, Is.EqualTo(0.1f).Within(1e-5f));
        }

        [Test]
        public void Castle_shows_its_damaged_picture_under_half_health()
        {
            StartBattle();

            _body.Hit(0, 500f);
            Assert.That(_spawner.View.Damaged, Is.False, "500 of 1000 is not under half");
            _body.Hit(0, 1f);
            Assert.That(_spawner.View.Damaged, Is.True);

            _upgrades.ApplyLevel(CastleUpgrade.Health, 3); // 799 of 1300
            Assert.That(_spawner.View.Damaged, Is.False);

            _run.StartRun();
            Assert.That(_spawner.View.Damaged, Is.False);
        }

        // feature-popup-revive/revive-order/order-values: the castle's part of the revive flow.

        [Test]
        public void Revive_anim_sets_thirty_percent_health_at_once_then_grows_back_over_half_a_second_and_tells_the_run()
        {
            StartBattle();
            _upgrades.ApplyLevel(CastleUpgrade.Health, 2); // max 1150
            _body.Hit(0, 5000f);

            _run.StartRevive();
            Assert.That(_body.Health, Is.EqualTo(345f), "30% of 1150, set at the start of the grow-back");
            Assert.That(_spawner.View.GrowBackSeconds, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(_spawner.View.Calls, Does.Contain("grow"));

            _spawner.View.Tick(0.375f);
            Assert.That(_run.ReviveShownCalls, Is.EqualTo(0), "still growing");
            _spawner.View.Tick(0.125f); // 0.375 + 0.125 = 0.5 exactly
            Assert.That(_run.ReviveShownCalls, Is.EqualTo(1), "told once the castle is full size");
            _spawner.View.Tick(1f);
            Assert.That(_run.ReviveShownCalls, Is.EqualTo(1), "told only once");
        }

        [Test]
        public void Edge_bang_gia_dang_lam_cham_quai_while_waiting_for_the_revive_the_castle_slows_nothing_more_and_its_cooldowns_stand_still()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            _skills.TryUse(CastleSkill.Frost);
            Assert.That(_enemies.Slows, Has.Count.EqualTo(1));
            _clock.Run(2f);
            _body.Hit(0, 5000f);
            _run.Step = RunStep.ReviveWait;
            _run.IsPaused = true;

            _clock.Run(3f, gameplay: false);
            _run.StartRevive();
            _clock.Run(0.5f, gameplay: false);

            Assert.That(_enemies.Slows, Has.Count.EqualTo(1), "the castle starts no new slow; the slow left is the enemies' own, counted in gameplay steps");
            Assert.That(_skills.GetState(CastleSkill.Frost).CooldownLeft, Is.EqualTo(33f).Within(1e-4f));
        }

        private void StartBattle()
        {
            _run.StartRun();
            _run.StartBattle();
        }

        /// <summary>Pause as the run does it: the run says paused and the clock's steps stop being gameplay.</summary>
        private void PausedSteps(float seconds)
        {
            _run.Step = RunStep.Paused;
            _run.IsPaused = true;
            _clock.Run(seconds, gameplay: false);
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
        public void Castle_steps_on_the_shared_clock_under_its_own_name()
        {
            Assert.That(_clock.Owners, Is.EqualTo(new[] { "Castle" }));
        }

        [Test]
        public void Frame_time_alone_no_longer_moves_the_battle()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            _skills.TryUse(CastleSkill.Thunder);
            var damageBefore = _enemies.Damaged.Count;

            _spawner.View.Tick(5f);

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(damageBefore), "no shot without a clock step");
            Assert.That(_skills.GetState(CastleSkill.Thunder).CooldownLeft, Is.EqualTo(20f).Within(1e-4f));
            Assert.That(_spawner.View.Calls, Does.Contain("advance"), "the drawing still runs on frames");
        }

        [Test]
        public void Steps_the_run_holds_still_do_not_move_the_castle()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            _skills.TryUse(CastleSkill.Thunder);
            var damageBefore = _enemies.Damaged.Count;

            _clock.Run(5f, gameplay: false);

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(damageBefore));
            Assert.That(_skills.GetState(CastleSkill.Thunder).CooldownLeft, Is.EqualTo(20f).Within(1e-4f));
        }

        [Test]
        public void Thunder_cooldown_of_twenty_seconds_ends_on_the_eight_hundredth_step()
        {
            StartBattle();
            _skills.TryUse(CastleSkill.Thunder);

            _clock.Steps(799);
            Assert.That(_skills.GetState(CastleSkill.Thunder).Ready, Is.False);
            _clock.Steps(1);
            Assert.That(_skills.GetState(CastleSkill.Thunder).Ready, Is.True);
        }

        [Test]
        public void A_long_battle_of_steps_keeps_the_shot_on_every_fortieth_step()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);

            _clock.Steps(1 + 40 * 600); // ten minutes

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(601), "one shot at once, then one per 40 steps without drift");
        }

        [Test]
        public void Ending_the_session_stops_listening_to_the_clock()
        {
            _session.DisposeAsync();

            Assert.That(_clock.SubscriberCount, Is.EqualTo(0));
        }

        [Test]
        public void Castle_shoots_only_once_the_battle_started()
        {
            _enemies.AddEnemy(1, 3f);
            _run.StartRun();
            _clock.Run(1f);
            Assert.That(_enemies.Damaged, Is.Empty);

            _run.StartBattle();
            _clock.Steps(1);

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(1));
            Assert.That(_spawner.View.Calls, Does.Contain("shot 1"));
        }

        [Test]
        public void Nothing_moves_while_paused()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            _spawner.View.Calls.Clear();

            PausedSteps(1f);
            _spawner.View.Tick(1f);

            Assert.That(_enemies.Damaged, Is.Empty);
            Assert.That(_spawner.View.Calls, Does.Not.Contain("advance"));
        }

        [Test]
        public void Shot_timer_stands_still_while_paused_and_goes_on_from_there()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            _clock.Steps(1);
            _clock.Run(0.5f);

            PausedSteps(10f);
            _run.IsPaused = false;
            _clock.Run(0.45f);
            Assert.That(_enemies.Damaged, Has.Count.EqualTo(1), "the paused 10s did not count");

            _clock.Run(0.05f);
            Assert.That(_enemies.Damaged, Has.Count.EqualTo(2));
        }

        [Test]
        public void Castle_stops_shooting_when_the_battle_stops()
        {
            StartBattle();
            _run.StopBattle();
            _enemies.AddEnemy(1, 3f);

            _clock.Run(1f);

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

            _clock.Run(5f);

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

            _clock.Steps(1);

            Assert.That(_enemies.Damaged, Is.EqualTo(new[] { (1, 10f) }), "damage is dealt when the shot fires");
            Assert.That(_spawner.View.ShotSeconds, Is.EqualTo(0.15f).Within(1e-5f));
        }

        [Test]
        public void Bullet_fly_time_of_every_shot_drawing_is_fifteen_hundredths_of_a_second()
        {
            // ShotParams bullet_fly_time = 0.15 s, the same for each shot.
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            var seconds = new List<float>();

            _clock.Steps(1);
            seconds.Add(_spawner.View.ShotSeconds);
            _spawner.View.ShotSeconds = 0f;
            _clock.Run(1f);
            seconds.Add(_spawner.View.ShotSeconds);

            Assert.That(seconds, Is.All.EqualTo(0.15f).Within(1e-5f));
        }

        [Test]
        public void Bullet_visual_is_one_drawing_per_shot_flying_straight_from_the_castle_edge_to_the_target()
        {
            // ShotParams bullet_visual (IDEA): one sprite moving straight for 0.15 s.
            StartBattle();
            _enemies.AddEnemy(7, 4f);
            _enemies.AddEnemy(8, 5f);

            _clock.Steps(1);

            Assert.That(_spawner.View.Calls.FindAll(c => c.StartsWith("shot")), Is.EqualTo(new[] { "shot 7" }));
            Assert.That(_spawner.View.ShotFromX, Is.EqualTo(1.5f), "starts at the castle edge on the target's side");
            Assert.That(_spawner.View.ShotToX, Is.EqualTo(4f), "ends at the target");
        }

        [Test]
        public void Shot_drawing_vanishes_when_its_target_is_defeated_by_someone_else()
        {
            StartBattle();
            _enemies.AddEnemy(1, 3f);
            _clock.Steps(1);
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
            _clock.Steps(1);

            _clock.Run(3f); // the 3s rest between waves: no battle signal arrives

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(4), "one shot at once, then one each second of the rest");
        }

        [Test]
        public void Skill_cooldown_keeps_running_into_the_next_wave()
        {
            StartBattle();
            _skills.TryUse(CastleSkill.Thunder);

            _clock.Run(3f);
            _clock.Run(10f);

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

            _clock.Run(0.4f, gameplay: false);
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
            _clock.Run(0.6f, gameplay: false);
            _spawner.View.Tick(0.6f);

            _run.Step = RunStep.Playing;
            _run.IsPaused = false;
            _clock.Steps(1);

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
        public void Fall_keeps_shrinking_after_the_revive_popup_opened_before_the_half_second_was_over()
        {
            // Tester PROTOTYPE 2: the body stood at scale 0.6 when the revive popup opened early.
            StartBattle();
            _body.Hit(0, 5000f);
            _run.Step = RunStep.ReviveWait;
            _run.IsPaused = true;
            _spawner.View.Tick(0.2f);

            _run.Step = RunStep.ReviveOffer;
            _spawner.View.Calls.Clear();
            _spawner.View.Tick(0.1f);

            Assert.That(_spawner.View.Calls, Does.Contain("advance"), "the fall's drawing goes on while the revive popup is up");
        }

        [Test]
        public void Fall_keeps_shrinking_while_the_run_waits_before_the_lose_popup()
        {
            StartBattle();
            _body.Hit(0, 5000f);
            _run.Step = RunStep.Ending;
            _run.IsPaused = true;
            _spawner.View.Calls.Clear();

            _spawner.View.Tick(0.1f);

            Assert.That(_spawner.View.Calls, Does.Contain("advance"));
        }

        [Test]
        public void Ending_the_session_removes_the_view()
        {
            _session.DisposeAsync();

            Assert.That(_spawner.Despawned, Is.True);
        }
    }
}
