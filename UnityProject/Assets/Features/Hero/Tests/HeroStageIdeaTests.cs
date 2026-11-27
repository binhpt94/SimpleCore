using Game.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Hero.Tests
{
    /// <summary>
    /// The IDEA column of "Thông số theo giai đoạn" of live feature-hero/hero-respawn/respawn-values,
    /// hero-stats/hero-level-values and hero-targeting/targeting-values (#stage-idea), one test per row.
    /// </summary>
    public sealed class HeroStageIdeaTests
    {
        private const float Tolerance = 1e-4f;
        private const float Step = 0.025f;
        private const string PrefabPath = "Assets/Features/Hero/FeatureAssets/Private/Hero.prefab";

        private HeroFakes.EnemyField _field;
        private HeroSquad _squad;
        private HeroFakes.DisplayFactory _displays;
        private HeroPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _field = new HeroFakes.EnemyField();
            var config = HeroFakes.LoadConfig();
            _squad = new HeroSquad(config, _field);
            _displays = new HeroFakes.DisplayFactory();
            _presenter = new HeroPresenter(_squad, config, _displays);
            _presenter.Attach();
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        private void Start(params string[] heroCodes) => _squad.Begin(new RunTeam(heroCodes));

        private HeroUnit Hero(int slot) => _squad.Units[slot];

        private void Run(float seconds)
        {
            var steps = (int)System.Math.Round(seconds / Step);
            for (var i = 0; i < steps; i++)
            {
                _squad.Tick(Step);
                _presenter.Refresh(Step);
            }
        }

        private static float ViewField(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Missing {PrefabPath}");
            var view = prefab.GetComponent<IHeroDisplay>() as Component;
            Assert.That(view, Is.Not.Null, "The hero prefab has no hero view");
            var property = new SerializedObject(view).FindProperty(name);
            Assert.That(property, Is.Not.Null, $"Hero view has no field {name}");
            return property.floatValue;
        }

        // ------------------------------------------------------------------ respawn-values#stage-idea

        [Test]
        public void At_idea_a_fallen_hero_waits_8s_before_it_respawns()
        {
            Start("knight");
            _squad.Hit(0, 300f);

            Run(7.975f);
            Assert.That(Hero(0).IsStanding, Is.False, "still waiting just before 8s");
            Assert.That(_displays.Made[0].Respawns, Is.EqualTo(0));

            Run(0.05f);
            Assert.That(Hero(0).IsStanding, Is.True, "back after 8s");
            Assert.That(_displays.Made[0].Respawns, Is.EqualTo(1));
        }

        [Test]
        public void At_idea_a_falling_hero_shrinks_away_in_0_3s()
        {
            Start("knight");

            _squad.Hit(0, 300f);

            Assert.That(_displays.Made[0].Falls, Is.EqualTo(1));
            Assert.That(_displays.Made[0].FallSeconds, Is.EqualTo(0.3f).Within(Tolerance));
        }

        [Test]
        public void At_idea_a_respawning_hero_grows_back_in_0_3s()
        {
            Start("knight");
            _squad.Hit(0, 300f);

            Run(8.05f);

            Assert.That(_displays.Made[0].Respawns, Is.EqualTo(1));
            Assert.That(_displays.Made[0].RespawnGrowSeconds, Is.EqualTo(0.3f).Within(Tolerance));
        }

        // ------------------------------------------------------------------ hero-level-values#stage-idea

        [Test]
        public void At_idea_each_level_adds_15_percent_of_level_1_health_and_damage()
        {
            Start("knight");

            _squad.ApplyLevel(0, 3);

            Assert.That(Hero(0).Stats.MaxHealth, Is.EqualTo(300f * 1.3f).Within(1e-3f));
            Assert.That(Hero(0).Stats.Damage, Is.EqualTo(12f * 1.3f).Within(1e-3f));
        }

        [Test]
        public void At_idea_each_level_adds_3_percent_attack_speed_up_to_60_percent()
        {
            Start("archer");

            _squad.ApplyLevel(0, 11);
            Assert.That(Hero(0).Stats.SecondsPerAttack, Is.EqualTo(0.8f / 1.3f).Within(1e-4f), "level 11: +30%");

            _squad.ApplyLevel(0, 21);
            Assert.That(Hero(0).Stats.SecondsPerAttack, Is.EqualTo(0.8f / 1.6f).Within(1e-4f), "level 21: +60%");

            _squad.ApplyLevel(0, 30);
            Assert.That(Hero(0).Stats.SecondsPerAttack, Is.EqualTo(0.8f / 1.6f).Within(1e-4f), "capped at +60%");
        }

        [Test]
        public void At_idea_a_level_up_pops_the_hero_to_1_2_times_and_back_in_0_3s()
        {
            Start("knight");

            _squad.ApplyLevel(0, 2);

            Assert.That(_displays.Made[0].LevelUps, Is.EqualTo(1));
            Assert.That(_displays.Made[0].PopScale, Is.EqualTo(1.2f).Within(Tolerance));
            Assert.That(_displays.Made[0].PopSeconds, Is.EqualTo(0.3f).Within(Tolerance));
            Assert.That(HeroLevelUpPop.ScaleAt(0.15f, 1.2f, 0.3f), Is.EqualTo(1.2f).Within(Tolerance), "peak half way");
            Assert.That(HeroLevelUpPop.ScaleAt(0f, 1.2f, 0.3f), Is.EqualTo(1f), "back to normal after 0.3s");
        }

        [Test]
        public void At_idea_a_ranged_hit_shows_one_sprite_flying_straight_from_the_hero_to_the_target_in_0_15s()
        {
            Start("archer");                       // x = -1.5, front edge -1.0
            _field.Add(5f);                        // gap 5.6u, in range 7u
            HeroStrike strike = default;
            var strikes = 0;
            _squad.Struck += (_, s) => { strike = s; strikes++; };

            Run(Step);

            Assert.That(strikes, Is.EqualTo(1));
            Assert.That(_displays.Made[0].Strikes, Is.EqualTo(1));
            Assert.That(strike.Group, Is.EqualTo(HeroGroup.Ranged));
            Assert.That(strike.Attack, Is.EqualTo(HeroAttack.SingleTarget));
            Assert.That(strike.FrontX, Is.EqualTo(-1f).Within(Tolerance), "flies from the hero's front edge");
            Assert.That(strike.TargetX, Is.EqualTo(5f).Within(Tolerance), "to the target's centre");
            Assert.That(ViewField("projectileSeconds"), Is.EqualTo(0.15f).Within(Tolerance), "flight time");
        }

        // ------------------------------------------------------------------ targeting-values#stage-idea

        [Test]
        public void At_idea_heroes_target_within_16u_and_walk_within_15u()
        {
            var config = HeroFakes.LoadConfig();
            Assert.That(config.TargetHalfRange, Is.EqualTo(16f).Within(Tolerance), "target zone");
            Assert.That(config.WalkHalfRange, Is.EqualTo(15f).Within(Tolerance), "walk zone");

            Start("knight");
            _field.Add(16.5f);                     // off screen: not targeted
            Run(0.5f);
            Assert.That(Hero(0).X, Is.EqualTo(-1.5f).Within(Tolerance), "ignores an enemy beyond 16u");

            // A knight of range 1.2u reaches any on-screen enemy before 15u; with range 0 and a thin enemy whose
            // inner edge is at 15.8u, the target stays out of reach and the hero waits at the 15u walk limit.
            var noReach = Object.Instantiate(config);
            try
            {
                noReach.Find("knight").range = 0f;
                var field = new HeroFakes.EnemyField();
                var squad = new HeroSquad(noReach, field);
                squad.Begin(new RunTeam(new[] { "knight" }));
                var far = field.Add(15.9f, halfWidth: 0.1f);
                for (var i = 0; i < 400; i++) squad.Tick(Step);   // 10s at 2.0u/s: more than the 16.5u walk

                Assert.That(squad.Units[0].X, Is.EqualTo(15f).Within(Tolerance), "stops at the 15u walk limit");
                Assert.That(field.Taken(far), Is.EqualTo(0f), "the target stays out of reach");
            }
            finally
            {
                Object.DestroyImmediate(noReach);
            }
        }

        [Test]
        public void At_idea_the_hero_sprite_faces_where_it_walks_or_its_target()
        {
            Start("knight");                       // x = -1.5
            var enemy = _field.Add(-8f);           // to the left, out of range

            Run(Step);
            Assert.That(Hero(0).IsMoving, Is.True);
            Assert.That(_displays.Made[0].Facing, Is.EqualTo(-1), "faces left while walking left");

            Run(3f);                               // now hitting the enemy on its left
            Assert.That(Hero(0).IsMoving, Is.False);
            Assert.That(_displays.Made[0].Facing, Is.EqualTo(-1), "faces its target while hitting");

            _field.Remove(enemy);
            Run(Step);                             // no enemy: walks home to the right
            Assert.That(_displays.Made[0].Facing, Is.EqualTo(1), "faces right while walking home");
        }

        [Test]
        public void At_idea_the_hero_view_flips_its_sprite_when_facing_left()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Missing {PrefabPath}");
            var instance = Object.Instantiate(prefab);
            try
            {
                var display = instance.GetComponent<IHeroDisplay>();
                display.Setup("knight", null, Color.white, 1f, 1.5f);
                var body = instance.transform.Find("Body").GetComponent<SpriteRenderer>();

                display.SetPose(0f, 1, true);
                Assert.That(body.flipX, Is.False, "drawn facing right");

                display.SetPose(0f, -1, true);
                Assert.That(body.flipX, Is.True, "flipped facing left");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
