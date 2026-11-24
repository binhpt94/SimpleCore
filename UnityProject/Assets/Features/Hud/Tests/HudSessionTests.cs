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

        private HudSession NewSession(HudCheat cheat = null) =>
            new HudSession(_spawner, _clock, _castle, _skills, _wallet, _waves, _run, HudTestTimings.Default, cheat);

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
        public void CheatButtonSetsTheMidBattleCoinsThroughTheWallet()
        {
            var cheat = new HudCheat(_run, _wallet, _wallet, _wallet);
            NewSession(cheat).Run();
            Assert.IsNotNull(_spawner.View.CheatLabel);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Hud cheat"));
            _spawner.View.PressCheat();

            Assert.AreEqual(HudCheat.MidBattleCoins, _wallet.Coins);
            Assert.AreEqual("245", _spawner.View.Coins);
        }

        [Test]
        public void CheatBringsCoinsDownThroughSpendingWhenThereAreMore()
        {
            _wallet.Set(400);
            var cheat = new HudCheat(_run, _wallet, _wallet, _wallet);

            cheat.ApplyMidBattle();

            Assert.AreEqual(HudCheat.MidBattleCoins, _wallet.Coins);
        }

        [Test]
        public void CheatDoesNothingOutsideARunningBattle()
        {
            _run.SetStep(RunStep.Home);
            var cheat = new HudCheat(_run, _wallet, _wallet, _wallet);

            cheat.ApplyMidBattle();

            Assert.AreEqual(50, _wallet.Coins);
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
            return registry;
        }
    }
}
