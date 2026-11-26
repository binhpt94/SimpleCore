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
            _skills = new CastleSkills(_config, _body, _enemies, _heroes, _enemies);
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
        public void Skills_cannot_be_used_after_the_castle_fell()
        {
            _body.Hit(0, 5000f);

            Assert.That(_skills.TryUse(CastleSkill.Thunder), Is.False);
        }

        [Test]
        public void Pressing_a_skill_on_cooldown_does_nothing_and_keeps_its_cooldown()
        {
            _enemies.AddEnemy(1, 3f);
            _skills.TryUse(CastleSkill.Thunder);
            _skills.Tick(5f);

            Assert.That(_skills.TryUse(CastleSkill.Thunder), Is.False);

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(1));
            Assert.That(_skills.GetState(CastleSkill.Thunder).CooldownLeft, Is.EqualTo(15f).Within(1e-4f));
        }

        [Test]
        public void Each_skill_has_its_own_cooldown_twenty_thirty_five_twenty_five()
        {
            _skills.TryUse(CastleSkill.Thunder);
            _skills.TryUse(CastleSkill.Frost);
            _skills.TryUse(CastleSkill.Rally);

            Assert.That(_skills.GetState(CastleSkill.Thunder).CooldownLeft, Is.EqualTo(20f));
            Assert.That(_skills.GetState(CastleSkill.Frost).CooldownLeft, Is.EqualTo(35f));
            Assert.That(_skills.GetState(CastleSkill.Rally).CooldownLeft, Is.EqualTo(25f));
        }

        [Test]
        public void Two_skills_pressed_in_one_frame_both_work_independently()
        {
            _enemies.AddEnemy(1, 3f);

            Assert.That(_skills.TryUse(CastleSkill.Thunder), Is.True);
            Assert.That(_skills.TryUse(CastleSkill.Frost), Is.True);

            Assert.That(_enemies.Damaged, Has.Count.EqualTo(1));
            Assert.That(_enemies.Slows, Has.Count.EqualTo(1));
            Assert.That(_skills.GetState(CastleSkill.Rally).Ready, Is.True);
        }

        [Test]
        public void Frost_on_already_slowed_enemies_asks_for_a_fresh_full_slow_not_a_bigger_one()
        {
            _skills.TryUse(CastleSkill.Frost);
            _skills.Tick(35f);

            _skills.TryUse(CastleSkill.Frost);

            Assert.That(_enemies.Slows, Is.EqualTo(new[] { 5f, 5f }));
        }

        [Test]
        public void Rally_and_frost_with_no_target_still_start_their_cooldown()
        {
            Assert.That(_skills.TryUse(CastleSkill.Frost), Is.True);
            Assert.That(_skills.TryUse(CastleSkill.Rally), Is.True);

            Assert.That(_skills.GetState(CastleSkill.Frost).Ready, Is.False);
            Assert.That(_skills.GetState(CastleSkill.Rally).Ready, Is.False);
        }

        [Test]
        public void Thunder_never_hurts_the_castle()
        {
            _enemies.AddEnemy(1, 2f);

            _skills.TryUse(CastleSkill.Thunder);

            Assert.That(_body.Health, Is.EqualTo(1000f));
        }

        [Test]
        public void Skill_with_no_cooldown_is_ready_again_right_after_use()
        {
            var settings = new UnityEditor.SerializedObject(_config);
            settings.FindProperty("thunderCooldown").floatValue = 0f;
            settings.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(_skills.TryUse(CastleSkill.Thunder), Is.True);

            var state = _skills.GetState(CastleSkill.Thunder);
            Assert.That(state.Ready, Is.True);
            Assert.That(state.CooldownLeft, Is.EqualTo(0f));
            Assert.That(_skills.TryUse(CastleSkill.Thunder), Is.True, "pressing again works without any tick");
        }

        [Test]
        public void Skills_level_bought_while_on_cooldown_keeps_the_cooldown_and_strengthens_the_next_use()
        {
            var upgrades = new CastleUpgrades(_body, _skills);
            _enemies.AddEnemy(1, 3f);
            _skills.TryUse(CastleSkill.Thunder);
            _skills.TryUse(CastleSkill.Frost);
            _skills.Tick(5f);

            upgrades.ApplyLevel(CastleUpgrade.Skills, 2);

            Assert.That(_skills.GetState(CastleSkill.Thunder).CooldownLeft, Is.EqualTo(15f).Within(1e-4f));
            Assert.That(_enemies.Slows, Is.EqualTo(new[] { 5f }), "enemies already slowed keep their old time");
            _skills.Tick(30f);
            _skills.TryUse(CastleSkill.Thunder);
            _skills.TryUse(CastleSkill.Frost);
            Assert.That(_enemies.Damaged[1].amount, Is.EqualTo(96f).Within(1e-4f));
            Assert.That(_enemies.Slows[1], Is.EqualTo(6f).Within(1e-4f));
        }

        [Test]
        public void Rally_at_skills_level_thirteen_asks_for_more_than_full_health_and_heroes_cap_it()
        {
            var upgrades = new CastleUpgrades(_body, _skills);
            upgrades.ApplyLevel(CastleUpgrade.Skills, 13);

            _skills.TryUse(CastleSkill.Rally);

            // 30% × (1 + 0.2 × 12) = 102%; IHeroSquad.HealAll caps at max health.
            Assert.That(_heroes.Heals[0], Is.EqualTo(1.02f).Within(1e-4f));
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
