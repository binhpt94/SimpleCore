using System;
using System.Collections.Generic;
using Game.Castle;
using Game.Hero;
using Game.Run;
using Game.Wallet;
using NUnit.Framework;

namespace Game.Upgrade.Tests
{
    public class UpgradeShopTests
    {
        private FakeRun _run;
        private FakeWallet _wallet;
        private FakeHeroes _heroes;
        private FakeCastle _castle;
        private UpgradeShopLogic _shop;
        private int _changes;

        [SetUp]
        public void SetUp()
        {
            _run = new FakeRun();
            _wallet = new FakeWallet();
            _heroes = new FakeHeroes();
            _castle = new FakeCastle();
            _shop = new UpgradeShopLogic(StandardPrices(), _run, _wallet, _wallet, _heroes, _castle);
            _shop.Attach();
            _shop.ItemsChanged += () => _changes++;
        }

        internal static UpgradePrices StandardPrices() => new UpgradePrices(3,
            new PriceCurve(30, 1.35), new PriceCurve(40, 1.4), new PriceCurve(40, 1.4), new PriceCurve(60, 1.5));

        private void EnterBattle(params string[] heroes)
        {
            _run.StartRun(new RunTeam(heroes.Length == 0 ? new[] { "knight", "archer", "mage" } : heroes));
        }

        [Test]
        public void Run_start_lists_three_heroes_in_slot_order_then_castle_health_attack_skills()
        {
            EnterBattle("knight", "archer", "mage");

            Assert.AreEqual(6, _shop.Count);
            Assert.AreEqual(UpgradeKind.Hero, _shop.Get(0).Kind);
            Assert.AreEqual("knight", _shop.Get(0).HeroCode);
            Assert.AreEqual(0, _shop.Get(0).HeroSlot);
            Assert.AreEqual("mage", _shop.Get(2).HeroCode);
            Assert.AreEqual(2, _shop.Get(2).HeroSlot);
            Assert.AreEqual(UpgradeKind.CastleHealth, _shop.Get(3).Kind);
            Assert.AreEqual(UpgradeKind.CastleAttack, _shop.Get(4).Kind);
            Assert.AreEqual(UpgradeKind.CastleSkills, _shop.Get(5).Kind);
            Assert.AreEqual(-1, _shop.Get(5).HeroSlot);
            for (var i = 0; i < 6; i++) Assert.AreEqual(1, _shop.Get(i).Level);
        }

        [Test]
        public void Two_heroes_of_the_same_kind_get_two_separate_items()
        {
            EnterBattle("knight", "knight", "mage");
            _wallet.Coins = 1000;

            _shop.Buy(1);

            Assert.AreEqual(1, _shop.Get(0).Level);
            Assert.AreEqual(2, _shop.Get(1).Level);
            Assert.AreEqual((1, 2), _heroes.Applied[0]);
        }

        [Test]
        public void A_team_of_the_wrong_size_fails_at_run_start_and_names_the_count()
        {
            var error = Assert.Throws<InvalidOperationException>(() => EnterBattle("knight", "archer"));
            StringAssert.Contains("2", error.Message);
        }

        [Test]
        public void Start_prices_are_30_for_heroes_40_for_castle_health_and_attack_60_for_skills()
        {
            EnterBattle();

            Assert.AreEqual(30, _shop.Get(0).NextPrice);
            Assert.AreEqual(40, _shop.Get(3).NextPrice);
            Assert.AreEqual(40, _shop.Get(4).NextPrice);
            Assert.AreEqual(60, _shop.Get(5).NextPrice);
        }

        [TestCase(1, 30)] [TestCase(2, 41)] [TestCase(3, 55)] [TestCase(4, 74)] [TestCase(5, 100)]
        [TestCase(6, 135)] [TestCase(7, 182)] [TestCase(8, 246)] [TestCase(9, 331)]
        public void Hero_price_follows_the_design_table(int level, int price)
        {
            Assert.AreEqual(price, new PriceCurve(30, 1.35).PriceAt(level));
        }

        [TestCase(1, 40)] [TestCase(2, 56)] [TestCase(3, 79)] [TestCase(4, 110)] [TestCase(5, 154)]
        [TestCase(6, 216)] [TestCase(7, 302)] [TestCase(8, 422)] [TestCase(9, 591)]
        public void Castle_health_and_attack_price_follows_the_design_table(int level, int price)
        {
            Assert.AreEqual(price, new PriceCurve(40, 1.4).PriceAt(level));
        }

        [TestCase(1, 60)] [TestCase(2, 90)] [TestCase(3, 135)] [TestCase(4, 203)] [TestCase(5, 304)]
        [TestCase(6, 456)] [TestCase(7, 684)] [TestCase(8, 1026)] [TestCase(9, 1538)]
        public void Castle_skills_price_follows_the_design_table(int level, int price)
        {
            Assert.AreEqual(price, new PriceCurve(60, 1.5).PriceAt(level));
        }

        [Test]
        public void A_huge_level_price_stays_a_whole_number_without_overflow()
        {
            Assert.AreEqual(int.MaxValue, new PriceCurve(60, 1.5).PriceAt(500));
        }

        [Test]
        public void Buying_pays_the_price_raises_the_level_and_shows_the_next_price()
        {
            EnterBattle();
            _wallet.Coins = 100;

            var result = _shop.Buy(0);

            Assert.AreEqual(PurchaseResult.Bought, result);
            Assert.AreEqual(70, _wallet.Coins);
            Assert.AreEqual(2, _shop.Get(0).Level);
            Assert.AreEqual(41, _shop.Get(0).NextPrice);
        }

        [Test]
        public void Buying_a_hero_level_tells_the_hero_in_that_slot_at_once()
        {
            EnterBattle();
            _wallet.Coins = 1000;

            _shop.Buy(2);
            _shop.Buy(2);

            CollectionAssert.AreEqual(new[] { (2, 2), (2, 3) }, _heroes.Applied);
        }

        [Test]
        public void Buying_castle_items_tells_the_castle_which_part_and_level()
        {
            EnterBattle();
            _wallet.Coins = 1000;

            _shop.Buy(3);
            _shop.Buy(4);
            _shop.Buy(5);

            CollectionAssert.AreEqual(
                new[] { (CastleUpgrade.Health, 2), (CastleUpgrade.Attack, 2), (CastleUpgrade.Skills, 2) },
                _castle.Applied);
        }

        [Test]
        public void Not_enough_coins_changes_nothing()
        {
            EnterBattle();
            _wallet.Coins = 29;

            var result = _shop.Buy(0);

            Assert.AreEqual(PurchaseResult.NotEnoughCoins, result);
            Assert.AreEqual(29, _wallet.Coins);
            Assert.AreEqual(1, _shop.Get(0).Level);
            Assert.IsEmpty(_heroes.Applied);
        }

        [Test]
        public void Exactly_enough_coins_buys_and_leaves_zero()
        {
            EnterBattle();
            _wallet.Coins = 30;

            Assert.AreEqual(PurchaseResult.Bought, _shop.Buy(0));
            Assert.AreEqual(0, _wallet.Coins);
        }

        [Test]
        public void Two_fast_taps_with_coins_for_one_buy_once_and_refuse_the_second()
        {
            EnterBattle();
            _wallet.Coins = 50;

            Assert.AreEqual(PurchaseResult.Bought, _shop.Buy(0));
            Assert.AreEqual(PurchaseResult.NotEnoughCoins, _shop.Buy(0));
            Assert.AreEqual(20, _wallet.Coins);
            Assert.AreEqual(2, _shop.Get(0).Level);
        }

        [Test]
        public void Each_item_has_its_own_price_by_its_own_level()
        {
            EnterBattle();
            _wallet.Coins = 1000;

            _shop.Buy(0);

            Assert.AreEqual(41, _shop.Get(0).NextPrice);
            Assert.AreEqual(30, _shop.Get(1).NextPrice);
        }

        [Test]
        public void Buying_while_paused_is_refused_even_with_enough_coins()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            _run.SetStep(RunStep.Paused);

            Assert.AreEqual(PurchaseResult.Unavailable, _shop.Buy(0));
            Assert.AreEqual(1000, _wallet.Coins);
            Assert.IsFalse(_shop.Get(0).CanBuy);
        }

        [Test]
        public void After_the_castle_falls_buying_is_refused()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            _run.Stop();

            Assert.AreEqual(PurchaseResult.Unavailable, _shop.Buy(3));
            Assert.AreEqual(1000, _wallet.Coins);
            Assert.IsFalse(_shop.Get(3).CanBuy);
        }

        [Test]
        public void Buying_before_any_run_or_with_a_wrong_index_is_refused()
        {
            _wallet.Coins = 1000;
            Assert.AreEqual(PurchaseResult.Unavailable, _shop.Buy(0));

            EnterBattle();
            Assert.AreEqual(PurchaseResult.Unavailable, _shop.Buy(6));
            Assert.AreEqual(PurchaseResult.Unavailable, _shop.Buy(-1));
        }

        [Test]
        public void Can_buy_follows_the_coins()
        {
            EnterBattle();
            _wallet.Coins = 35;

            Assert.IsTrue(_shop.Get(0).CanBuy);
            Assert.IsFalse(_shop.Get(3).CanBuy);
        }

        [Test]
        public void The_bar_is_told_when_coins_change_pause_toggles_or_a_level_is_bought()
        {
            EnterBattle();
            _changes = 0;

            _wallet.Grant(10);
            Assert.AreEqual(1, _changes);

            _run.SetStep(RunStep.Paused);
            Assert.AreEqual(2, _changes);

            _run.SetStep(RunStep.Playing);
            _wallet.Coins = 100;
            _changes = 0;
            _shop.Buy(0);
            Assert.GreaterOrEqual(_changes, 1);
        }

        [Test]
        public void A_new_run_puts_every_item_back_to_level_1_with_the_new_team()
        {
            EnterBattle("knight", "archer", "mage");
            _wallet.Coins = 1000;
            _shop.Buy(0);
            _shop.Buy(5);
            _run.Stop();

            EnterBattle("mage", "priest", "archer");

            Assert.AreEqual(6, _shop.Count);
            Assert.AreEqual("mage", _shop.Get(0).HeroCode);
            for (var i = 0; i < 6; i++) Assert.AreEqual(1, _shop.Get(i).Level);
            Assert.AreEqual(PurchaseResult.Bought, _shop.Buy(0));
        }

        [Test]
        public void After_detach_the_shop_no_longer_follows_the_run()
        {
            _shop.Detach();

            EnterBattle();

            Assert.AreEqual(0, _shop.Count);
        }

        // ---- Fakes of the other features -------------------------------------------------------------------

        private sealed class FakeRun : IRunFlow
        {
            public RunStep Step { get; private set; } = RunStep.Home;
            public RunTeam Team { get; private set; }
            public bool IsPaused => Step == RunStep.Paused;

            public event Action<RunStep> StepChanged;
            public event Action<RunTeam> RunStarted;
            public event Action BattleStarted;
            public event Action BattleStopped;
            public event Action BattleCleared;

            public void StartRun(RunTeam team)
            {
                Team = team;
                SetStep(RunStep.Playing);
                RunStarted?.Invoke(team);
                BattleStarted?.Invoke();
            }

            public void Stop()
            {
                SetStep(RunStep.Ending);
                BattleStopped?.Invoke();
            }

            public void SetStep(RunStep step)
            {
                Step = step;
                StepChanged?.Invoke(step);
            }

            public void Clear() => BattleCleared?.Invoke();

            public void Play() { }
            public void EnterBattle(RunTeam team) { }
            public void Pause() { }
            public void Resume() { }
            public void Abandon() { }
            public void Replay() { }
            public void GoHome() { }
            public void Lose() { }
            public void CountClearedWave(int wave) { }
        }

        private sealed class FakeWallet : IWalletService, IWalletBalance
        {
            private int _coins;

            public event Action<int> CoinsChanged;

            public int Coins
            {
                get => _coins;
                set => _coins = value;
            }

            public void Grant(int amount)
            {
                _coins += amount;
                CoinsChanged?.Invoke(_coins);
            }

            public SpendResult TrySpend(int amount)
            {
                if (amount <= 0) return SpendResult.InvalidAmount;
                if (amount > _coins) return SpendResult.NotEnoughCoins;
                _coins -= amount;
                CoinsChanged?.Invoke(_coins);
                return SpendResult.Ok;
            }
        }

        private sealed class FakeHeroes : IHeroSquad
        {
            public readonly List<(int slot, int level)> Applied = new List<(int, int)>();

            public void ApplyLevel(int slot, int level) => Applied.Add((slot, level));
            public void HealAll(float fractionOfMaxHealth) { }
        }

        private sealed class FakeCastle : ICastleUpgrades
        {
            public readonly List<(CastleUpgrade item, int level)> Applied = new List<(CastleUpgrade, int)>();

            public void ApplyLevel(CastleUpgrade item, int level) => Applied.Add((item, level));
        }
    }
}
