using Game.Run;
using NUnit.Framework;

namespace Game.Hud.Tests
{
    public sealed class HudStatusTests
    {
        private FakeView _view;
        private FakeCastle _castle;
        private FakeWallet _wallet;
        private FakeWaves _waves;
        private FakeRun _run;
        private HudStatusPresenter _status;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeView();
            _castle = new FakeCastle();
            _wallet = new FakeWallet();
            _waves = new FakeWaves();
            _run = new FakeRun();
            _status = new HudStatusPresenter(_view, _castle, _wallet, _waves, _run, HudTestTimings.Default);
            _status.Start();
        }

        [Test]
        public void BattleStartShowsFullHealthStartCoinsAndWaveOne()
        {
            Assert.AreEqual("1000/1000", _view.Health);
            Assert.AreEqual(1f, _view.HealthFill);
            Assert.AreEqual("50", _view.Coins);
            Assert.AreEqual("Đợt 1", _view.Wave);
            Assert.IsFalse(_view.BannerShown);
        }

        [Test]
        public void LosingHealthShortensTheBarAndUpdatesTheNumber()
        {
            _castle.Set(750f, 1000f);

            Assert.AreEqual("750/1000", _view.Health);
            Assert.AreEqual(0.75f, _view.HealthFill, 1e-5f);
        }

        [Test]
        public void HealthNumberRoundsDownSoAlmostDeadShowsZeroWithASliverOfBar()
        {
            _castle.Set(0.4f, 1000f);

            Assert.AreEqual("0/1000", _view.Health);
            Assert.Greater(_view.HealthFill, 0f);
        }

        [Test]
        public void HealthBelowZeroShowsZeroAndAnEmptyBar()
        {
            _castle.Set(-30f, 1000f);

            Assert.AreEqual("0/1000", _view.Health);
            Assert.AreEqual(0f, _view.HealthFill);
        }

        [Test]
        public void HealthAboveMaxFillsTheBarAndShowsTheRealNumber()
        {
            _castle.Set(1200f, 1000f);

            Assert.AreEqual("1200/1000", _view.Health);
            Assert.AreEqual(1f, _view.HealthFill);
        }

        [Test]
        public void RaisingMaxHealthUpdatesNumberAndBarAtOnce()
        {
            _castle.Set(750f, 1000f);
            _castle.Set(850f, 1100f);

            Assert.AreEqual("850/1100", _view.Health);
            Assert.AreEqual(850f / 1100f, _view.HealthFill, 1e-5f);
        }

        [Test]
        public void CoinsChangeTheMomentTheWalletChanges()
        {
            _wallet.Set(12345);

            Assert.AreEqual("12345", _view.Coins);
        }

        [Test]
        public void WaveBoxFollowsTheCurrentOrNextWave()
        {
            _waves.SetCurrent(8);

            Assert.AreEqual("Đợt 8", _view.Wave);
        }

        [Test]
        public void WaveStartShowsTheBannerForOneAndAHalfSeconds()
        {
            _waves.Start(7);

            Assert.IsTrue(_view.BannerShown);
            Assert.AreEqual("Đợt 7", _view.Banner);

            _status.Tick(1.4f);
            Assert.IsTrue(_view.BannerShown);

            _status.Tick(0.2f);
            Assert.IsFalse(_view.BannerShown);
        }

        [Test]
        public void BannerTimeStandsStillWhilePaused()
        {
            _waves.Start(2);
            _status.Tick(1f);
            _run.SetStep(RunStep.Paused);
            _status.Tick(5f);

            Assert.IsTrue(_view.BannerShown);

            _run.SetStep(RunStep.Playing);
            _status.Tick(0.6f);
            Assert.IsFalse(_view.BannerShown);
        }

        [Test]
        public void NewWaveWhileBannerIsUpReplacesItAndRestartsTheTime()
        {
            _waves.Start(2);
            _status.Tick(1.2f);
            _waves.Start(3);
            _status.Tick(1.2f);

            Assert.IsTrue(_view.BannerShown);
            Assert.AreEqual("Đợt 3", _view.Banner);
        }

        [Test]
        public void RunEndingHidesTheBannerAndKeepsTheLastValues()
        {
            _castle.Set(0f, 1000f);
            _waves.Start(4);
            _run.StopBattle();

            Assert.IsFalse(_view.BannerShown);
            Assert.AreEqual("0/1000", _view.Health);
        }

        [Test]
        public void StoppedPresenterNoLongerFollowsTheCastle()
        {
            _status.Stop();
            _castle.Set(100f, 1000f);

            Assert.AreEqual("1000/1000", _view.Health);
            Assert.AreEqual(0, _castle.Listeners);
        }
    }
}
