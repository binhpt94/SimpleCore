using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Castle.Tests
{
    public class CastleSessionTests
    {
        private sealed class FakeView : ICastleView
        {
            public readonly List<string> Calls = new List<string>();
            public bool Visible;
            public event Action<float> Ticked;
            public void Tick(float seconds) => Ticked?.Invoke(seconds);
            public void Place(float halfWidth, float height) => Calls.Add($"place {halfWidth} {height}");
            public void SetVisible(bool visible) => Visible = visible;
            public void ResetBody() => Calls.Add("reset");
            public void FlashHit(float seconds) => Calls.Add("flash");
            public void Collapse(float seconds) => Calls.Add("collapse");
            public void ShowShot(int targetHandle, float fromX, float toX, float seconds) => Calls.Add($"shot {targetHandle}");
            public void DropShotsAt(int targetHandle) => Calls.Add($"drop {targetHandle}");
            public void ShowThunder(float x, float seconds) => Calls.Add($"thunder {x}");
            public void Advance(float seconds) => Calls.Add("advance");
            public void ClearEffects() => Calls.Add("clear");
        }

        private sealed class FakeSpawner : ICastleViewSpawner
        {
            public readonly FakeView View = new FakeView();
            public bool Despawned;
            public ICastleView Spawn() => View;
            public void Despawn(ICastleView view) => Despawned = true;
        }

        private CastleConfig _config;
        private FakeRunFlow _run;
        private FakeEnemyField _enemies;
        private CastleBody _body;
        private CastleSkills _skills;
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
            var upgrades = new CastleUpgrades(_body, _skills);
            var autoShot = new CastleAutoShot(_config, _body, _enemies, upgrades);
            _spawner = new FakeSpawner();
            _session = new CastleSession(_config, _run, _enemies, _body, _skills, upgrades, autoShot, _spawner);
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
        public void Ending_the_session_removes_the_view()
        {
            _session.DisposeAsync();

            Assert.That(_spawner.Despawned, Is.True);
        }
    }
}
