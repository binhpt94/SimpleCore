using NUnit.Framework;
using UnityEngine;

namespace Game.Castle.Tests
{
    public class CastleCheatTests
    {
        private CastleConfig _config;
        private FakeRunFlow _run;
        private CastleBody _body;
        private CastleSkills _skills;
        private CastleCheat _cheat;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CastleConfig>();
            _run = new FakeRunFlow();
            _body = new CastleBody(_config, _run);
            var enemies = new FakeEnemyField();
            _skills = new CastleSkills(_config, _body, enemies, new FakeHeroSquad(), enemies);
            _cheat = new CastleCheat(_body, _skills);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Cheat_sets_castle_health_and_reports_it()
        {
            var changed = 0;
            _body.HealthChanged += () => changed++;

            _cheat.SetHealth(300f);

            Assert.That(_body.Health, Is.EqualTo(300f));
            Assert.That(_body.MaxHealth, Is.EqualTo(1000f));
            Assert.That(changed, Is.EqualTo(1));
        }

        [Test]
        public void Cheat_health_never_destroys_nor_overfills_the_castle()
        {
            _cheat.SetHealth(-50f);
            Assert.That(_body.Health, Is.EqualTo(1f));
            Assert.That(_body.IsDestroyed, Is.False);
            Assert.That(_run.LoseCalls, Is.EqualTo(0));

            _cheat.SetHealth(5000f);
            Assert.That(_body.Health, Is.EqualTo(1000f));
        }

        [Test]
        public void Cheat_health_is_ignored_once_the_castle_fell()
        {
            _body.Hit(0, 5000f);

            _cheat.SetHealth(300f);

            Assert.That(_body.Health, Is.EqualTo(0f));
            Assert.That(_body.IsDestroyed, Is.True);
        }

        [Test]
        public void Cheat_puts_a_skill_on_cooldown_without_using_it()
        {
            var struck = 0;
            _skills.ThunderStruck += _ => struck++;

            _cheat.SetCooldownLeft(CastleSkill.Thunder, 12f);

            var state = _skills.GetState(CastleSkill.Thunder);
            Assert.That(state.CooldownLeft, Is.EqualTo(12f));
            Assert.That(state.Ready, Is.False);
            Assert.That(struck, Is.EqualTo(0));
        }

        [Test]
        public void Cheat_cooldown_is_clamped_and_zero_makes_the_skill_ready()
        {
            _cheat.SetCooldownLeft(CastleSkill.Thunder, 999f);
            Assert.That(_skills.GetState(CastleSkill.Thunder).CooldownLeft, Is.EqualTo(20f));

            _cheat.SetCooldownLeft(CastleSkill.Thunder, 0f);
            Assert.That(_skills.GetState(CastleSkill.Thunder).Ready, Is.True);
        }

        // ICastleFallCheat.

        [Test]
        public void Fall_cheat_brings_the_castle_to_zero_and_tells_the_run_once_like_a_real_fall()
        {
            var destroyed = 0;
            _body.Destroyed += () => destroyed++;
            var fall = new CastleFallCheat(_body, () => true);

            fall.Fall();

            Assert.That(_body.Health, Is.EqualTo(0f));
            Assert.That(_body.IsDestroyed, Is.True);
            Assert.That(_run.LoseCalls, Is.EqualTo(1));
            Assert.That(destroyed, Is.EqualTo(1));
        }

        [Test]
        public void Fall_cheat_on_a_castle_with_a_fraction_of_health_still_falls()
        {
            _body.Hit(0, 999.6f);
            var fall = new CastleFallCheat(_body, () => true);

            fall.Fall();

            Assert.That(_body.IsDestroyed, Is.True);
            Assert.That(_run.LoseCalls, Is.EqualTo(1));
        }

        [Test]
        public void Fall_cheat_does_nothing_to_a_castle_already_fallen()
        {
            _body.Hit(0, 5000f);
            var fall = new CastleFallCheat(_body, () => true);

            fall.Fall();

            Assert.That(_run.LoseCalls, Is.EqualTo(1), "the run was told once, by the real fall");
        }

        [Test]
        public void Fall_cheat_does_nothing_when_no_battle_is_running()
        {
            var fall = new CastleFallCheat(_body, () => false);

            fall.Fall();

            Assert.That(_body.IsDestroyed, Is.False);
            Assert.That(_body.Health, Is.EqualTo(1000f));
            Assert.That(_run.LoseCalls, Is.EqualTo(0));
        }

    }
}
