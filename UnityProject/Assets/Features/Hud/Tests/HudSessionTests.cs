using System;
using System.Threading;
using Game.Castle;
using Game.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Hud.Tests
{
    public sealed class HudSessionTests
    {
        private const string InstallerPath = "Assets/Features/Hud/FeatureAssets/Public/HudInstaller.asset";

        private FakeSpawner _spawner;
        private FakeClock _clock;
        private FakeCastle _castle;
        private FakeSkills _skills;
        private FakeWallet _wallet;
        private FakeWaves _waves;
        private FakeRun _run;

        [SetUp]
        public void SetUp()
        {
            _spawner = new FakeSpawner();
            _clock = new FakeClock();
            _castle = new FakeCastle();
            _skills = new FakeSkills();
            _wallet = new FakeWallet();
            _waves = new FakeWaves();
            _run = new FakeRun();
        }

        private HudSession NewSession(HudCheat cheat = null, bool pauseButton = true, HudTimings? timings = null) =>
            new HudSession(_spawner, _clock, _castle, _skills, _wallet, _waves, _run, timings ?? HudTestTimings.Default, pauseButton, cheat);

        private const string ConfigPath = "Assets/Features/Hud/FeatureAssets/Private/HudConfig.asset";

        [Test]
        public void WhileRunningTheHudReportsItsViewAndFrameLoopAndAfterEndingNothing()
        {
            var session = NewSession();
            var usage = (Game.Core.ISessionParticipantUsage)session;
            Assert.AreEqual(0, usage.HeldViews);
            Assert.AreEqual(0, usage.RunningJobs);

            session.Run();
            Assert.AreEqual(1, usage.HeldViews, "the HUD view");
            Assert.AreEqual(1, usage.RunningJobs, "the per-frame clock loop");
            // The game opens on the start screen; the HUD belongs to the battle, so the session's run step
            // does not promise a held HUD view.
            Assert.IsFalse(usage.OpensViewsOnRun);

            session.DisposeAsync();
            Assert.AreEqual(0, usage.HeldViews);
            Assert.AreEqual(0, usage.RunningJobs);
            Assert.IsFalse(_clock.Running);
        }

        [Test]
        public void AtPrototypeTheWaveBannerShowsForOneAndAHalfSecondsFromTheRealConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<HudConfig>(ConfigPath);
            Assert.IsNotNull(config, "missing " + ConfigPath);
            Assert.AreEqual(1.5f, config.Timings.BannerSeconds, 1e-4f, "PROTOTYPE: 'Thông báo Đợt N đầu đợt' is 'Có, 1.5 giây'");

            NewSession(timings: config.Timings).Run();
            _waves.Start(3);
            Assert.IsTrue(_spawner.View.BannerShown);
            Assert.AreEqual("Đợt 3", _spawner.View.Banner);

            _clock.Tick(1.4f);
            Assert.IsTrue(_spawner.View.BannerShown);
            _clock.Tick(0.2f);
            Assert.IsFalse(_spawner.View.BannerShown);
        }

        [Test]
        public void AtPrototypeAllThreeSkillButtonsShowCoverAndBounceFromTheRealConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<HudConfig>(ConfigPath);
            Assert.IsNotNull(config, "missing " + ConfigPath);
            Assert.AreEqual(1.2f, config.Timings.BounceScale, 1e-4f, "PROTOTYPE: bounce to 1.2x");
            Assert.AreEqual(0.3f, config.Timings.BounceSeconds, 1e-4f, "PROTOTYPE: bounce lasts 0.3 s");

            NewSession(timings: config.Timings).Run();
            var view = _spawner.View;
            CollectionAssert.AreEqual(new[] { "Sấm sét", "Băng giá", "Cổ vũ" }, view.SkillNames, "PROTOTYPE: 3 buttons");

            // Frost with 12 of 35 seconds left: seconds label and a cover for the share left.
            _skills.SetCooling(CastleSkill.Frost, 12f, 35f);
            _clock.Tick(0.016f);
            Assert.AreEqual("12", view.Skills[1].Seconds);
            Assert.AreEqual(12f / 35f, view.Skills[1].Cover, 1e-3f);

            // Cooldown ends: the button grows towards 1.2x and is back to normal within 0.3 s.
            _skills.SetReady(CastleSkill.Frost);
            _clock.Tick(0.016f);
            _clock.Tick(0.13f);
            Assert.Greater(view.Skills[1].Scale, 1.1f);
            _clock.Tick(0.2f);
            Assert.AreEqual(1f, view.Skills[1].Scale);
        }

        [Test]
        public void AtPrototypeTheConfigTurnsThePauseButtonOnAndATapReportsPause()
        {
            var config = AssetDatabase.LoadAssetAtPath<HudConfig>(ConfigPath);
            Assert.IsNotNull(config, "missing " + ConfigPath);
            Assert.IsTrue(config.ShowsPauseButton, "PROTOTYPE: 'Có nút tạm dừng' is optional; this game turns it on");

            NewSession(pauseButton: config.ShowsPauseButton).Run();
            _spawner.View.PressPause();

            Assert.IsTrue(_spawner.View.PauseShown);
            Assert.AreEqual(1, _run.PauseRequests);
        }

        [Test]
        public void WithThePauseButtonOffTheHudHasNoneAndAPauseTapDoesNothing()
        {
            var session = NewSession(pauseButton: false);
            session.Run();
            _spawner.View.PressPause();

            Assert.IsFalse(_spawner.View.PauseShown);
            Assert.AreEqual(0, _spawner.View.PauseListeners);
            Assert.AreEqual(0, _run.PauseRequests);
        }

        [Test]
        public void WithThePauseButtonOnItIsShownAndReportsPause()
        {
            NewSession(pauseButton: true).Run();
            _spawner.View.PressPause();

            Assert.IsTrue(_spawner.View.PauseShown);
            Assert.AreEqual(1, _run.PauseRequests);
        }

        private FakeCastleCheat _castleCheat;

        private HudCheat NewCheat()
        {
            _castleCheat = new FakeCastleCheat(_castle, _skills);
            return new HudCheat(_run, _wallet, _wallet, _wallet, _skills, _castleCheat, _waves);
        }

        [Test]
        public void NoHudExistsBeforeTheSessionRuns()
        {
            var session = NewSession();
            session.LoadAsync(CancellationToken.None);

            Assert.AreEqual(0, _spawner.Spawned);
        }

        [Test]
        public void RunningSessionShowsTheHudWithStartValues()
        {
            NewSession().Run();

            Assert.AreEqual(1, _spawner.Spawned);
            Assert.AreEqual("1000/1000", _spawner.View.Health);
            Assert.AreEqual("50", _spawner.View.Coins);
            Assert.AreEqual("Đợt 1", _spawner.View.Wave);
            Assert.AreEqual(3, _spawner.View.SkillNames.Count);
            Assert.IsTrue(_clock.Running);
        }

        [Test]
        public void HudIsHiddenOutsideTheBattleScreens()
        {
            NewSession().Run();

            _run.SetStep(RunStep.Home);
            Assert.IsFalse(_spawner.View.Visible);
            _run.SetStep(RunStep.TeamSelect);
            Assert.IsFalse(_spawner.View.Visible);
            _run.SetStep(RunStep.Playing);
            Assert.IsTrue(_spawner.View.Visible);
            _run.SetStep(RunStep.Lost);
            Assert.IsTrue(_spawner.View.Visible);
        }

        [Test]
        public void EveryFrameTheButtonsShowTheCastlesCooldown()
        {
            NewSession().Run();
            _skills.SetCooling(CastleSkill.Frost, 12f, 35f);

            _clock.Tick(0.016f);

            Assert.AreEqual("12", _spawner.View.Skills[1].Seconds);
        }

        [Test]
        public void EndingTheSessionRemovesTheHudAndStopsListening()
        {
            var session = NewSession();
            session.Run();

            session.DisposeAsync();
            _castle.Set(10f, 1000f);

            Assert.IsTrue(_spawner.View.Destroyed);
            Assert.IsFalse(_clock.Running);
            Assert.AreEqual(0, _castle.Listeners);
            Assert.AreEqual(0, _spawner.View.SkillListeners);
            Assert.IsNull(session.View);
        }

        [Test]
        public void PauseButtonWorksFromBattleStartUntilTheBattleIsRemoved()
        {
            var session = NewSession();
            session.Run();
            Assert.AreEqual(1, _spawner.View.PauseListeners);

            _spawner.View.PressPause();
            Assert.AreEqual(1, _run.PauseRequests);

            // Paused, ending and lost are battle steps: the HUD, and with it the button, stays on screen.
            foreach (var step in new[] { RunStep.Paused, RunStep.Ending, RunStep.Lost })
            {
                _run.SetStep(step);
                Assert.IsTrue(_spawner.View.Visible, step.ToString());
            }

            session.DisposeAsync();
            _spawner.View.PressPause();

            Assert.AreEqual(0, _spawner.View.PauseListeners);
            Assert.AreEqual(1, _run.PauseRequests);
        }

        [Test]
        public void CheatButtonShowsTheWholeMidBattleState()
        {
            NewSession(NewCheat()).Run();
            Assert.IsNotNull(_spawner.View.CheatLabel);

            _spawner.View.PressCheat();
            _clock.Tick(0.016f);

            Assert.AreEqual("300/1000", _spawner.View.Health);
            Assert.AreEqual(0.3f, _spawner.View.HealthFill, 1e-5f);
            Assert.AreEqual("245", _spawner.View.Coins);
            Assert.AreEqual("Đợt 7", _spawner.View.Wave);
            Assert.IsTrue(_spawner.View.BannerShown);
            Assert.AreEqual("Đợt 7", _spawner.View.Banner);
            Assert.IsTrue(_spawner.View.Skills[0].Ready);
            Assert.IsFalse(_spawner.View.Skills[1].Ready);
            Assert.AreEqual("12", _spawner.View.Skills[1].Seconds);
            Assert.AreEqual(12f / 35f, _spawner.View.Skills[1].Cover, 1e-5f);
            Assert.IsTrue(_spawner.View.Skills[2].Ready);
        }

        [Test]
        public void CheatStartsTheWaveBeforeSettingHealthAndCooldowns()
        {
            var cheat = NewCheat();
            var log = new System.Collections.Generic.List<string>();
            _waves.Calls = log;
            _castleCheat.Calls = log;

            cheat.ApplyMidBattle();

            CollectionAssert.AreEqual(
                new[] { "wave 7", "health", "cooldown Thunder", "cooldown Frost", "cooldown Rally" }, log);
        }

        [Test]
        public void CheatReportsSkillsTheBattleDoesNotHaveInsteadOfSettingThem()
        {
            _skills.Skills = new[] { CastleSkill.Thunder };
            var cheat = NewCheat();

            var missing = cheat.ApplyMidBattle();

            CollectionAssert.AreEqual(new[] { "cooldown Thunder" },
                _castleCheat.Calls.FindAll(c => c.StartsWith("cooldown")));
            Assert.AreEqual(2, missing.Count);
            StringAssert.Contains("Frost", missing[0]);
            StringAssert.Contains("Rally", missing[1]);
        }

        [Test]
        public void CheatWarnsAboutSkippedPartsWhenPressed()
        {
            _skills.Skills = new[] { CastleSkill.Thunder };
            NewSession(NewCheat()).Run();

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Hud cheat.*Frost"));
            _spawner.View.PressCheat();
        }

        [Test]
        public void CheatBringsCoinsDownThroughSpendingWhenThereAreMore()
        {
            _wallet.Set(400);

            NewCheat().ApplyMidBattle();

            Assert.AreEqual(HudCheat.MidBattleCoins, _wallet.Coins);
        }

        [Test]
        public void CheatDoesNothingOutsideARunningBattle()
        {
            _run.SetStep(RunStep.Home);
            var cheat = NewCheat();

            cheat.ApplyMidBattle();

            Assert.AreEqual(50, _wallet.Coins);
            Assert.AreEqual(1000f, _castle.Health);
            Assert.IsEmpty(_waves.Calls);
        }

        [Test]
        public void InstallerAssetJoinsTheSessionWithoutCreatingObjects()
        {
            var installer = AssetDatabase.LoadAssetAtPath<HudInstaller>(InstallerPath);
            Assert.IsNotNull(installer, "missing " + InstallerPath);
            var registry = FullRegistry(out var scene);

            installer.Install(registry);

            Assert.AreEqual(1, scene.Joined.Count);
            Assert.IsInstanceOf<HudSession>(scene.Joined[0]);
        }

        [Test]
        public void InstallerFailsAtOnceNamingAMissingDependency()
        {
            var installer = AssetDatabase.LoadAssetAtPath<HudInstaller>(InstallerPath);
            Assert.IsNotNull(installer, "missing " + InstallerPath);
            var registry = new FakeRegistry();
            registry.Register<Game.Core.ISessionScene>(new FakeSessionScene());

            var error = Assert.Throws<InvalidOperationException>(() => installer.Install(registry));
            StringAssert.Contains("IRunFlow", error.Message);
        }

        private FakeRegistry FullRegistry(out FakeSessionScene scene)
        {
            scene = new FakeSessionScene();
            var registry = new FakeRegistry();
            registry.Register<Game.Core.ISessionScene>(scene);
            registry.Register<IRunFlow>(_run);
            registry.Register<ICastle>(_castle);
            registry.Register<ICastleSkills>(_skills);
            registry.Register<Game.Wallet.IWalletBalance>(_wallet);
            registry.Register<Game.Wallet.IWalletIncome>(_wallet);
            registry.Register<Game.Wallet.IWalletService>(_wallet);
            registry.Register<Game.Wave.IWaveProgress>(_waves);
            registry.Register<ICastleCheat>(new FakeCastleCheat(_castle, _skills));
            registry.Register<Game.Wave.IWaveCheat>(_waves);
            return registry;
        }
    }
}
