using NUnit.Framework;
using UnityEngine;

namespace Game.Castle.Tests
{
    public class CastleSkillsTests
    {
        private CastleConfig _config;
        private FakeRunFlow _run;
        private FakeEnemyField _enemies;
        private FakeHeroSquad _heroes;
        private CastleBody _body;
        private CastleSkills _skills;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CastleConfig>();
            _run = new FakeRunFlow();
            _enemies = new FakeEnemyField();
            _heroes = new FakeHeroSquad();
            _body = new CastleBody(_config, _run);
            _skills = new CastleSkills(_config, _body, _run, _enemies, _heroes);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Every_skill_is_ready_when_the_battle_starts()
        {
            foreach (var skill in _skills.Skills)
                Assert.That(_skills.GetState(skill).Ready, Is.True, skill.ToString());
        }

        [Test]
        public void Thunder_hits_only_enemies_within_six_units_of_the_castle_edge_on_both_sides()
        {
            _enemies.AddEnemy(1, 7.8f);   // gap 7.8 - 0.4 - 1.5 = 5.9: in
            _enemies.AddEnemy(2, -5f);    // in, left side
            _enemies.AddEnemy(3, 9f);     // gap 7.1: out

            Assert.That(_skills.TryUse(CastleSkill.Thunder), Is.True);

            Assert.That(_enemies.Damaged, Is.EquivalentTo(new[] { (1, 80f), (2, 80f) }));
        }

        [Test]
        public void Used_skill_cannot_be_pressed_again_until_its_cooldown_ends()
        {
            _skills.TryUse(CastleSkill.Thunder);

            Assert.That(_skills.TryUse(CastleSkill.Thunder), Is.False);
            _skills.Tick(19.9f);
            Assert.That(_skills.GetState(CastleSkill.Thunder).Ready, Is.False);
            _skills.Tick(0.1f);
            Assert.That(_skills.TryUse(CastleSkill.Thunder), Is.True);
        }

        [Test]
        public void Pressing_with_no_target_still_uses_the_skill()
        {
            Assert.That(_skills.TryUse(CastleSkill.Thunder), Is.True);
            Assert.That(_skills.GetState(CastleSkill.Thunder).CooldownLeft, Is.EqualTo(20f));
        }

        [Test]
        public void Frost_slows_every_enemy_and_rally_heals_every_hero()
        {
            _skills.TryUse(CastleSkill.Frost);
            _skills.TryUse(CastleSkill.Rally);

            Assert.That(_enemies.Slows, Is.EqualTo(new[] { 5f }));
            Assert.That(_heroes.Heals, Is.EqualTo(new[] { 0.3f }));
        }

        [Test]
        public void Skills_cannot_be_used_while_paused_and_cooldowns_stand_still()
        {
            _skills.TryUse(CastleSkill.Rally);
            _run.IsPaused = true;

            Assert.That(_skills.TryUse(CastleSkill.Frost), Is.False);
            _skills.Tick(10f);
            Assert.That(_skills.GetState(CastleSkill.Rally).CooldownLeft, Is.EqualTo(25f));
        }

        [Test]
        public void Skills_cannot_be_used_after_the_castle_fell()
        {
            _body.Hit(0, 5000f);

            Assert.That(_skills.TryUse(CastleSkill.Thunder), Is.False);
        }

        [Test]
        public void Skills_level_strengthens_all_three_effects_by_a_fifth_per_level()
        {
            var upgrades = new CastleUpgrades(_body, _skills);
            upgrades.ApplyLevel(CastleUpgrade.Skills, 2);
            _enemies.AddEnemy(1, 3f);

            _skills.TryUse(CastleSkill.Thunder);
            _skills.TryUse(CastleSkill.Frost);
            _skills.TryUse(CastleSkill.Rally);

            Assert.That(_enemies.Damaged[0].amount, Is.EqualTo(96f).Within(1e-4f));
            Assert.That(_enemies.Slows[0], Is.EqualTo(6f).Within(1e-4f));
            Assert.That(_heroes.Heals[0], Is.EqualTo(0.36f).Within(1e-4f));
        }
    }
}
