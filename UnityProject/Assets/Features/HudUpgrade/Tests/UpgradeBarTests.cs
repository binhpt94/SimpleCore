using System.Linq;
using Game.Run;
using NUnit.Framework;

namespace Game.HudUpgrade.Tests
{
    public sealed class UpgradeBarTests
    {
        private FakeWallet _wallet;
        private FakeShop _shop;
        private FakeRun _run;
        private FakeBarView _view;
        private UpgradeBarPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _wallet = new FakeWallet { Coins = 50 };
            _shop = new FakeShop(_wallet);
            _shop.SetTeam("knight", "archer", "mage");
            _run = new FakeRun();
            _view = new FakeBarView();
            _presenter = new UpgradeBarPresenter(_shop, new FakeRoster(), _run, _view);
            _presenter.Start();
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        [Test]
        public void Bar_has_six_buttons_heroes_in_team_order_then_health_attack_skills()
        {
            Assert.AreEqual(6, _view.Pictures.Count);
            CollectionAssert.AreEqual(new[] { "Hiệp sĩ", "Cung thủ", "Pháp sư", "Máu", "Sức đánh", "Kĩ năng" },
                _view.Pictures.Select(p => p.Caption).ToArray());
            CollectionAssert.AreEqual(new[] { "knight", "archer", "mage", null, null, null },
                _view.Pictures.Select(p => p.HeroCode).ToArray());
        }

        [Test]
        public void Hero_buttons_and_castle_buttons_are_two_separate_groups()
        {
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 1, 1 }, _view.Pictures.Select(p => p.Group).ToArray());
        }

        [Test]
        public void Each_button_shows_level_and_next_price()
        {
            Assert.AreEqual("Cấp 1", _view.Looks[0].LevelText);
            Assert.AreEqual("30", _view.Looks[0].PriceText);
            Assert.AreEqual("60", _view.Looks[5].PriceText);
        }

        [Test]
        public void Button_is_lit_when_affordable_and_dim_when_not()
        {
            Assert.IsTrue(_view.Looks[0].Lit);   // 30 <= 50
            Assert.IsFalse(_view.Looks[5].Lit);  // 60 > 50
        }

        [Test]
        public void Buttons_light_up_in_the_same_call_coins_arrive()
        {
            _wallet.Add(20);
            _shop.Changed();
            Assert.IsTrue(_view.Looks[5].Lit);
        }

        [Test]
        public void Every_button_dims_while_shop_is_closed_by_pause()
        {
            _shop.Open = false;
            _shop.Changed();
            Assert.IsTrue(_view.Looks.Values.All(l => !l.Lit));
        }

        [Test]
        public void Tapping_a_lit_button_buys_one_level_bounces_and_shows_new_level()
        {
            _view.Tap(0);
            Assert.AreEqual(1, _shop.BuyCalls);
            CollectionAssert.AreEqual(new[] { 0 }, _view.Bounces);
            Assert.AreEqual("Cấp 2", _view.Looks[0].LevelText);
            Assert.AreEqual("40", _view.Looks[0].PriceText);
        }

        [Test]
        public void Tapping_a_dim_button_sends_no_purchase_and_no_bounce()
        {
            _view.Tap(5);
            Assert.AreEqual(0, _shop.BuyCalls);
            Assert.IsEmpty(_view.Bounces);
        }

        [Test]
        public void Two_fast_taps_with_coins_for_one_buy_only_once_and_bounce_once()
        {
            _wallet.Coins = 30;
            _shop.Changed();
            _view.Tap(0);
            _view.Tap(0);
            Assert.AreEqual(1, _view.Bounces.Count);
            Assert.IsFalse(_view.Looks[0].Lit);
        }

        [Test]
        public void Rejected_purchase_does_not_bounce()
        {
            _shop.RejectBuys = true;
            _view.Tap(0);
            Assert.AreEqual(1, _shop.BuyCalls);
            Assert.IsEmpty(_view.Bounces);
        }

        [Test]
        public void Bar_shows_in_battle_pause_and_ending_and_hides_on_lose_popup_and_menus()
        {
            Assert.IsFalse(_view.Visible);
            _run.GoTo(RunStep.Playing);
            Assert.IsTrue(_view.Visible);
            _run.GoTo(RunStep.Paused);
            Assert.IsTrue(_view.Visible);
            _run.GoTo(RunStep.Ending);
            Assert.IsTrue(_view.Visible);
            _run.GoTo(RunStep.Lost);
            Assert.IsFalse(_view.Visible);
            _run.GoTo(RunStep.TeamSelect);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void New_run_with_another_team_rebuilds_the_hero_buttons()
        {
            var builds = _view.Builds;
            _shop.SetTeam("axeman", "archer", "mage");
            Assert.AreEqual(builds + 1, _view.Builds);
            Assert.AreEqual("Đấu sĩ rìu", _view.Pictures[0].Caption);
        }

        [Test]
        public void Level_or_price_change_does_not_rebuild_the_buttons()
        {
            var builds = _view.Builds;
            _view.Tap(0);
            Assert.AreEqual(builds, _view.Builds);
        }

        [Test]
        public void Big_prices_read_short_on_the_button()
        {
            _shop.SetPrice(5, 12_399);
            Assert.AreEqual("12,3K", _view.Looks[5].PriceText);
        }

        [Test]
        public void Bar_stops_listening_after_dispose()
        {
            _presenter.Dispose();
            _wallet.Coins = 1000;
            _shop.Changed();
            Assert.IsFalse(_view.Looks[5].Lit);
        }
    }

    public sealed class PriceTextTests
    {
        [TestCase(55, "55")]
        [TestCase(9_999, "9999")]
        [TestCase(10_000, "10,0K")]
        [TestCase(12_399, "12,3K")]
        [TestCase(181_000, "181,0K")]
        [TestCase(999_999, "999,9K")]
        [TestCase(1_000_000, "1,0M")]
        [TestCase(1_250_000, "1,2M")]
        public void Price_reads_in_full_below_ten_thousand_and_cut_down_above(int price, string expected)
        {
            Assert.AreEqual(expected, PriceText.Format(price));
        }
    }
}
