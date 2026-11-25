using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Hero.Tests
{
    /// <summary>
    /// One test per row of the design tables of live feature-hero/hero-list/hero-roster-values (.excel: Heroes, AttackKinds,
    /// Effects, StageParams). Each test names the row key it checks, so a row added or changed in the design shows up here.
    /// </summary>
    public sealed class HeroRosterTableTests
    {
        private const float Tolerance = 1e-4f;
        private const string PrefabPath = "Assets/Features/Hero/FeatureAssets/Private/Hero.prefab";

        private HeroFakes.EnemyField _field;
        private HeroSquad _squad;

        [SetUp]
        public void SetUp()
        {
            _field = new HeroFakes.EnemyField();
            _squad = new HeroSquad(HeroFakes.LoadConfig(), _field);
        }

        // ------------------------------------------------------------------ Heroes

        private static void AssertHeroRow(
            string key, HeroConfig.Stage stage, HeroGroup group, float maxHealth, float damage, float attackInterval,
            float attackRange, float moveSpeed, HeroAttack attackKind, float areaSize)
        {
            var config = HeroFakes.LoadConfig();
            var hero = config.Find(key);

            Assert.That(hero, Is.Not.Null, key);
            Assert.That(hero.stage, Is.EqualTo(stage), $"{key} stage");
            Assert.That(hero.group, Is.EqualTo(group), $"{key} group");
            Assert.That(hero.maxHealth, Is.EqualTo(maxHealth).Within(Tolerance), $"{key} max_health");
            Assert.That(hero.damage, Is.EqualTo(damage).Within(Tolerance), $"{key} damage");
            Assert.That(hero.secondsPerAttack, Is.EqualTo(attackInterval).Within(Tolerance), $"{key} attack_interval");
            Assert.That(hero.range, Is.EqualTo(attackRange).Within(Tolerance), $"{key} attack_range");
            Assert.That(hero.moveSpeed, Is.EqualTo(moveSpeed).Within(Tolerance), $"{key} move_speed");
            Assert.That(hero.attack, Is.EqualTo(attackKind), $"{key} attack_kind");
            Assert.That(hero.areaSize, Is.EqualTo(areaSize).Within(Tolerance), $"{key} area_size");
            Assert.That(config.BoxWidth, Is.EqualTo(1f).Within(Tolerance), $"{key} box_width");
            Assert.That(config.BoxHeight, Is.EqualTo(1.5f).Within(Tolerance), $"{key} box_height");
        }

        [Test]
        public void Heroes_knight_is_an_idea_melee_single_target_with_300_health_12_damage_every_1s_range_1_2_speed_2() =>
            AssertHeroRow("knight", HeroConfig.Stage.Idea, HeroGroup.Melee, 300f, 12f, 1f, 1.2f, 2f, HeroAttack.SingleTarget, 0f);

        [Test]
        public void Heroes_axeman_is_an_idea_melee_front_cleave_of_1_5u_with_240_health_14_damage_every_1_4s_range_1_3_speed_2_4() =>
            AssertHeroRow("axeman", HeroConfig.Stage.Idea, HeroGroup.Melee, 240f, 14f, 1.4f, 1.3f, 2.4f, HeroAttack.FrontCleave, 1.5f);

        [Test]
        public void Heroes_archer_is_an_idea_ranged_single_target_with_120_health_14_damage_every_0_8s_range_7_speed_3() =>
            AssertHeroRow("archer", HeroConfig.Stage.Idea, HeroGroup.Ranged, 120f, 14f, 0.8f, 7f, 3f, HeroAttack.SingleTarget, 0f);

        [Test]
        public void Heroes_swordsman_is_a_prototype_melee_single_target_with_180_health_16_damage_every_0_7s_range_1_2_speed_3_5() =>
            AssertHeroRow("swordsman", HeroConfig.Stage.Prototype, HeroGroup.Melee, 180f, 16f, 0.7f, 1.2f, 3.5f, HeroAttack.SingleTarget, 0f);

        [Test]
        public void Heroes_mage_is_a_prototype_ranged_blast_of_1_5u_with_100_health_18_damage_every_1_6s_range_6_speed_2_6() =>
            AssertHeroRow("mage", HeroConfig.Stage.Prototype, HeroGroup.Ranged, 100f, 18f, 1.6f, 6f, 2.6f, HeroAttack.Blast, 1.5f);

        [Test]
        public void Heroes_swordsman_and_mage_join_the_roster_at_prototype()
        {
            Assert.That(CodesAt(HeroConfig.Stage.Idea), Does.Not.Contain("swordsman").And.Not.Contain("mage"));
            Assert.That(CodesAt(HeroConfig.Stage.Prototype), Does.Contain("swordsman").And.Contain("mage"));
        }

        [Test]
        public void Heroes_swordsman_and_mage_fight_in_a_prototype_team()
        {
            _squad.Begin(new Game.Run.RunTeam(new[] { "swordsman", "mage", "knight" }));

            Assert.That(_squad.Units[0].Hero.code, Is.EqualTo("swordsman"));
            Assert.That(_squad.Units[1].Hero.code, Is.EqualTo("mage"));
            Assert.That(_squad.Units[0].Health, Is.EqualTo(180f).Within(Tolerance));
            Assert.That(_squad.Units[1].Health, Is.EqualTo(100f).Within(Tolerance));
        }

        // ------------------------------------------------------------------ AttackKinds

        [Test]
        public void AttackKinds_single_target_hits_only_the_target()
        {
            _squad.Begin(new Game.Run.RunTeam(new[] { "knight", "axeman", "archer" }));
            _squad.Hit(1, 1000f);
            _squad.Hit(2, 1000f);
            var target = _field.Add(0f);           // knight at -1.5: gap 0.6u, in range
            var behind = _field.Add(0.3f);         // touching the target, not hit by a single_target attack

            _squad.Tick(0.025f);

            Assert.That(_squad.Units[0].Hero.attack, Is.EqualTo(HeroAttack.SingleTarget), "knight uses single_target");
            Assert.That(_field.Taken(target), Is.EqualTo(12f).Within(Tolerance));
            Assert.That(_field.Taken(behind), Is.EqualTo(0f));
        }

        [Test]
        public void AttackKinds_front_cleave_hits_every_enemy_touching_the_1_5u_stretch_from_the_front_edge_towards_the_target()
        {
            _squad.Begin(new Game.Run.RunTeam(new[] { "axeman", "knight", "archer" }));
            _squad.Hit(1, 1000f);
            _squad.Hit(2, 1000f);
            // Axeman at -1.5 facing right once it picks the target; front edge at -1.0, stretch to +0.5.
            var target = _field.Add(-0.2f);
            var inStretch = _field.Add(0.8f);      // box from 0.4: touches the stretch
            var pastStretch = _field.Add(1.0f);    // box from 0.6: past the stretch
            var behind = _field.Add(-3.5f);        // on the other side of the axeman, farther than the target

            _squad.Tick(0.025f);

            Assert.That(_squad.Units[0].Hero.attack, Is.EqualTo(HeroAttack.FrontCleave), "axeman uses front_cleave");
            Assert.That(_field.Taken(target), Is.EqualTo(14f).Within(Tolerance));
            Assert.That(_field.Taken(inStretch), Is.EqualTo(14f).Within(Tolerance));
            Assert.That(_field.Taken(pastStretch), Is.EqualTo(0f));
            Assert.That(_field.Taken(behind), Is.EqualTo(0f));
        }

        /// <summary>A team whose slot 1 is the mage, alone on the field (slots 2 and 3 fall at once).</summary>
        private void StartWithOnlyTheMage()
        {
            _squad.Begin(new Game.Run.RunTeam(new[] { "mage", "knight", "archer" }));
            _squad.Hit(1, 1000f);
            _squad.Hit(2, 1000f);
        }

        [Test]
        public void AttackKinds_blast_hits_the_target_and_every_enemy_whose_box_edge_is_within_1_5u_of_its_centre_on_both_sides()
        {
            StartWithOnlyTheMage();
            // Mage at -1.5, range 6u: the target at 3 (gap 3.6u) is hit on the first step and kept while in range.
            var target = _field.Add(3f);
            _squad.Tick(0.025f);
            var nearSide = _field.Add(1.6f);       // edge 1.0u from the target centre, between mage and target
            var farSide = _field.Add(4.8f);        // edge 1.4u from the target centre, past the target
            var outside = _field.Add(5.2f);        // edge 1.8u: outside the blast

            for (var i = 0; i < 64; i++) _squad.Tick(0.025f);   // 1.6 s: the next blast

            Assert.That(_squad.Units[0].Hero.attack, Is.EqualTo(HeroAttack.Blast), "mage uses blast");
            Assert.That(_squad.Units[0].TargetHandle, Is.EqualTo(target), "blast keeps its target");
            Assert.That(_field.Taken(target), Is.EqualTo(36f).Within(Tolerance));
            Assert.That(_field.Taken(nearSide), Is.EqualTo(18f).Within(Tolerance));
            Assert.That(_field.Taken(farSide), Is.EqualTo(18f).Within(Tolerance));
            Assert.That(_field.Taken(outside), Is.EqualTo(0f));
        }

        // ------------------------------------------------------------------ Effects (numbers on the hero prefab's view)

        private static SerializedObject HeroViewFields()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Missing {PrefabPath}");
            var view = prefab.GetComponent<IHeroDisplay>() as Component;
            Assert.That(view, Is.Not.Null, "The hero prefab has no hero view");
            return new SerializedObject(view);
        }

        private static float Field(SerializedObject fields, string name)
        {
            var property = fields.FindProperty(name);
            Assert.That(property, Is.Not.Null, $"Hero view has no field {name}");
            return property.floatValue;
        }

        [Test]
        public void Effects_walk_bob_lifts_the_hero_0_1u_every_0_4s_while_it_walks()
        {
            var fields = HeroViewFields();

            Assert.That(Field(fields, "bobHeight"), Is.EqualTo(0.1f).Within(Tolerance), "walk_bob height");
            Assert.That(Field(fields, "bobPeriod"), Is.EqualTo(0.4f).Within(Tolerance), "walk_bob period");
        }

        [Test]
        public void Effects_walk_bob_shows_while_walking_and_stops_when_the_hero_stops()
        {
            var displays = new HeroFakes.DisplayFactory();
            var presenter = new HeroPresenter(_squad, HeroFakes.LoadConfig(), displays);
            presenter.Attach();
            _squad.Begin(new Game.Run.RunTeam(new[] { "knight", "axeman", "archer" }));
            _squad.Hit(1, 1000f);
            _squad.Hit(2, 1000f);
            var enemy = _field.Add(10f);

            _squad.Tick(0.025f);
            presenter.Refresh(0.025f);
            Assert.That(displays.Made[0].Moving, Is.True, "walk_bob appears when the hero starts walking");

            _field.Remove(enemy);
            _squad.Units[0].X = _squad.Units[0].SpawnX;
            _squad.Tick(0.025f);
            presenter.Refresh(0.025f);
            Assert.That(displays.Made[0].Moving, Is.False, "walk_bob disappears when the hero stops");
            presenter.Dispose();
        }

        [Test]
        public void Effects_attack_nudge_moves_the_hero_0_2u_towards_its_target_for_0_15s_on_each_hit()
        {
            var fields = HeroViewFields();

            Assert.That(Field(fields, "lungeDistance"), Is.EqualTo(0.2f).Within(Tolerance), "attack_nudge distance");
            Assert.That(Field(fields, "lungeSeconds"), Is.EqualTo(0.15f).Within(Tolerance), "attack_nudge seconds");
        }

        [Test]
        public void Effects_hit_flash_lasts_0_1s_and_each_new_hit_restarts_it()
        {
            var fields = HeroViewFields();
            var displays = new HeroFakes.DisplayFactory();
            var presenter = new HeroPresenter(_squad, HeroFakes.LoadConfig(), displays);
            presenter.Attach();
            _squad.Begin(new Game.Run.RunTeam(new[] { "knight", "axeman", "archer" }));

            _squad.Hit(0, 10f);
            _squad.Hit(0, 10f);

            Assert.That(Field(fields, "flashSeconds"), Is.EqualTo(0.1f).Within(Tolerance), "hit_flash seconds");
            Assert.That(displays.Made[0].Hurts, Is.EqualTo(2), "hit_flash plays on every hit");
            presenter.Dispose();
        }

        [Test]
        public void Effects_cleave_trail_of_the_axeman_covers_its_1_5u_stretch_for_0_2s()
        {
            var fields = HeroViewFields();
            var displays = new HeroFakes.DisplayFactory();
            var presenter = new HeroPresenter(_squad, HeroFakes.LoadConfig(), displays);
            presenter.Attach();
            _squad.Begin(new Game.Run.RunTeam(new[] { "axeman", "knight", "archer" }));
            _squad.Hit(1, 1000f);
            _squad.Hit(2, 1000f);
            _field.Add(0f);
            HeroStrike strike = default;
            _squad.Struck += (_, s) => strike = s;

            _squad.Tick(0.025f);

            Assert.That(Field(fields, "cleaveSeconds"), Is.EqualTo(0.2f).Within(Tolerance), "cleave_trail seconds");
            Assert.That(displays.Made[0].Strikes, Is.EqualTo(1), "cleave_trail shows on each hit");
            Assert.That(strike.Attack, Is.EqualTo(HeroAttack.FrontCleave));
            Assert.That(strike.AreaSize, Is.EqualTo(1.5f).Within(Tolerance), "cleave_trail covers 1.5u");
            presenter.Dispose();
        }

        [Test]
        public void Effects_blast_ring_of_the_mage_shows_1_5u_around_the_target_when_it_hits_and_goes_after_0_3s()
        {
            var fields = HeroViewFields();
            var displays = new HeroFakes.DisplayFactory();
            var presenter = new HeroPresenter(_squad, HeroFakes.LoadConfig(), displays);
            presenter.Attach();
            StartWithOnlyTheMage();
            _field.Add(3f);
            HeroStrike strike = default;
            _squad.Struck += (_, s) => strike = s;

            _squad.Tick(0.025f);

            Assert.That(Field(fields, "blastSeconds"), Is.EqualTo(0.3f).Within(Tolerance), "blast_ring seconds");
            Assert.That(displays.Made[0].Strikes, Is.EqualTo(1), "blast_ring shows when the blast hits");
            Assert.That(strike.Attack, Is.EqualTo(HeroAttack.Blast));
            Assert.That(strike.TargetX, Is.EqualTo(3f).Within(Tolerance), "blast_ring is centred on the target");
            Assert.That(strike.AreaSize, Is.EqualTo(1.5f).Within(Tolerance), "blast_ring radius 1.5u");
            presenter.Dispose();
        }

        // ------------------------------------------------------------------ StageParams (IDEA column)

        private static List<string> CodesAt(HeroConfig.Stage stage)
        {
            var codes = new List<string>();
            foreach (var hero in HeroFakes.LoadConfig().HeroesAt(stage)) codes.Add(hero.code);
            return codes;
        }

        [Test]
        public void StageParams_hero_count_is_3_at_idea_and_5_at_prototype()
        {
            Assert.That(CodesAt(HeroConfig.Stage.Idea), Is.EqualTo(new[] { "knight", "axeman", "archer" }), "hero_count idea");
            Assert.That(CodesAt(HeroConfig.Stage.Prototype), Has.Count.EqualTo(5), "hero_count prototype");
        }

        [Test]
        public void StageParams_team_slots_are_3_and_hold_all_three_idea_heroes()
        {
            var config = HeroFakes.LoadConfig();

            var slots = config.SlotSpawnX.Count;   // read through IReadOnlyList: the asset keeps an array

            Assert.That(slots, Is.EqualTo(3), "team_slots");
            Assert.That(CodesAt(HeroConfig.Stage.Idea).Count, Is.EqualTo(slots), "team_slots idea: no choice to make");
        }

        [Test]
        public void StageParams_attack_kinds_at_idea_are_single_target_and_front_cleave()
        {
            var kinds = new HashSet<HeroAttack>();
            foreach (var hero in HeroFakes.LoadConfig().HeroesAt(HeroConfig.Stage.Idea)) kinds.Add(hero.attack);

            Assert.That(kinds, Is.EquivalentTo(new[] { HeroAttack.SingleTarget, HeroAttack.FrontCleave }), "attack_kinds idea");
        }

        [Test]
        public void StageParams_stats_at_idea_are_the_heroes_table_numbers()
        {
            AssertHeroRow("knight", HeroConfig.Stage.Idea, HeroGroup.Melee, 300f, 12f, 1f, 1.2f, 2f, HeroAttack.SingleTarget, 0f);
            AssertHeroRow("axeman", HeroConfig.Stage.Idea, HeroGroup.Melee, 240f, 14f, 1.4f, 1.3f, 2.4f, HeroAttack.FrontCleave, 1.5f);
            AssertHeroRow("archer", HeroConfig.Stage.Idea, HeroGroup.Ranged, 120f, 14f, 0.8f, 7f, 3f, HeroAttack.SingleTarget, 0f);
        }

        [Test]
        public void StageParams_move_speed_at_idea_is_2_for_knight_2_4_for_axeman_3_for_archer()
        {
            var config = HeroFakes.LoadConfig();

            Assert.That(config.Find("knight").moveSpeed, Is.EqualTo(2f).Within(Tolerance), "move_speed knight");
            Assert.That(config.Find("axeman").moveSpeed, Is.EqualTo(2.4f).Within(Tolerance), "move_speed axeman");
            Assert.That(config.Find("archer").moveSpeed, Is.EqualTo(3f).Within(Tolerance), "move_speed archer");
        }

        [Test]
        public void StageParams_visuals_at_idea_give_each_hero_one_sprite()
        {
            foreach (var hero in HeroFakes.LoadConfig().HeroesAt(HeroConfig.Stage.Idea))
                Assert.That(hero.sprite, Is.Not.Null, $"visuals: {hero.code} has its sprite");
        }
    }
}
