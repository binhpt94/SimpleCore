using System.Linq;
using NUnit.Framework;
using static Game.SimClock.Tests.SimClockTestKit;

namespace Game.SimClock.Tests
{
    /// <summary>
    /// Rows of the ClockParams table (live: feature-sim-clock/clock-step/clock-values) against the real config
    /// asset, and the behaviour each row drives.
    /// </summary>
    public class ClockParamsTests
    {
        [Test]
        public void tick_rate_Is40StepsASecond()
        {
            var config = LoadConfig();
            Assert.That(config.tickRate, Is.EqualTo(40), "tick_rate");
            var clock = StartedClock(config);
            for (var i = 0; i < 8; i++) clock.Advance(0.125);
            Assert.That(clock.StepCount, Is.EqualTo(40));
        }

        [Test]
        public void step_seconds_Is1OverTickRate_0025()
        {
            var config = LoadConfig();
            Assert.That(config.StepSeconds, Is.EqualTo(0.025).Within(1e-12), "step_seconds");
            Assert.That(StartedClock(config).StepSeconds, Is.EqualTo(0.025f));
        }

        [Test]
        public void default_mode_IsFixedTick()
        {
            var config = LoadConfig();
            Assert.That(config.mode, Is.EqualTo(SimMode.FixedTick), "default_mode");
            Assert.That(StartedClock(config).Mode, Is.EqualTo(SimMode.FixedTick));
        }

        [Test]
        public void modes_FixedTickAndFrame_BothRun()
        {
            var config = NewConfig();
            try
            {
                foreach (var mode in new[] { SimMode.FixedTick, SimMode.Frame })
                {
                    config.mode = mode;
                    var clock = StartedClock(config);
                    Assert.That(clock.Mode, Is.EqualTo(mode), "modes");
                    Assert.That(clock.Advance(0.025), Is.EqualTo(1));
                }
            }
            finally
            {
                Destroy(config);
            }
        }

        [Test]
        public void max_steps_per_frame_Is5()
        {
            var config = LoadConfig();
            Assert.That(config.maxStepsPerFrame, Is.EqualTo(5), "max_steps_per_frame");
            Assert.That(StartedClock(config).Advance(1.0), Is.EqualTo(5));
        }

        [Test]
        public void max_frame_seconds_Is0125()
        {
            var config = LoadConfig();
            Assert.That(config.maxFrameSeconds, Is.EqualTo(0.125f), "max_frame_seconds");
            var clock = StartedClock(config);
            clock.Advance(10.0);
            Assert.That(clock.GameplaySeconds, Is.EqualTo(0.125).Within(1e-9));
        }

        [Test]
        public void dev_speeds_Are1_2_4_8_16()
        {
            var config = LoadConfig();
            Assert.That(config.devSpeeds, Is.EqualTo(new[] { 1, 2, 4, 8, 16 }), "dev_speeds");
        }

        [Test]
        public void dev_max_steps_per_frame_Is5TimesSpeed_80At16()
        {
            var config = LoadConfig();
            Assert.That(config.devSpeeds.Select(config.MaxStepsAt), Is.EqualTo(new[] { 5, 10, 20, 40, 80 }),
                "dev_max_steps_per_frame");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var clock = StartedClock(config);
            clock.SetSpeed(16);
            Assert.That(clock.Advance(0.125), Is.EqualTo(80));
#endif
        }

        [Test]
        public void dev_run_steps_IsOn_AndRunsNSteps()
        {
            var config = LoadConfig();
            Assert.That(config.devRunSteps, Is.True, "dev_run_steps");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var clock = StartedClock(config);
            clock.RunSteps(200);
            Assert.That(clock.StepCount, Is.EqualTo(200));
#endif
        }

        [Test]
        public void dev_freeze_step_IsOn_AndFreezesAndSteps()
        {
            var config = LoadConfig();
            Assert.That(config.devFreezeStep, Is.True, "dev_freeze_step");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var clock = StartedClock(config);
            clock.Freeze(true);
            Assert.That(clock.Advance(0.1), Is.EqualTo(0));
            clock.StepOnce();
            Assert.That(clock.StepCount, Is.EqualTo(1));
#endif
        }

        [Test]
        public void view_interpolation_IsOffAtIdea()
        {
            var config = LoadConfig();
            Assert.That(config.viewInterpolation, Is.False, "view_interpolation");
        }

        [Test]
        public void Params_RealConfigIsValid_AndKeepsTheStepOrder()
        {
            var config = LoadConfig();
            Assert.DoesNotThrow(config.Validate);
            Assert.That(config.stepOrder, Is.EqualTo(new[] { "Wave", "Enemy", "Hero", "Castle", "Combat", "Run" }));
        }

        [Test]
        public void Params_BrokenConfig_StopsWithTheFieldName()
        {
            var config = NewConfig();
            try
            {
                config.tickRate = 0;
                var ex = Assert.Throws<System.InvalidOperationException>(config.Validate);
                Assert.That(ex.Message, Does.Contain("tick_rate"));
            }
            finally
            {
                Destroy(config);
            }
        }
    }
}
