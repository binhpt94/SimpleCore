using System;
using System.Collections.Generic;
using Game.Castle;
using Game.Hero;
using Game.Run;
using Game.SimClock;
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
        private FakeClock _clock;
        private UpgradeShopLogic _shop;
        private int _changes;

        [SetUp]
        public void SetUp()
        {
            _run = new FakeRun();
            _wallet = new FakeWallet();
            _heroes = new FakeHeroes();
            _castle = new FakeCastle();
            _clock = new FakeClock();
            _shop = new UpgradeShopLogic(StandardPrices(), _run, _wallet, _wallet, _heroes, _castle, _clock);
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
        public void The_bar_is_told_again_when_play_resumes_after_a_pause()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            _run.SetStep(RunStep.Paused);
            _changes = 0;

            _run.SetStep(RunStep.Playing);

            Assert.GreaterOrEqual(_changes, 1);
            for (var i = 0; i < _shop.Count; i++) Assert.IsTrue(_shop.Get(i).CanBuy);
        }

        [Test]
        public void The_bar_is_told_when_the_castle_falls_and_every_item_turns_unbuyable()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            for (var i = 0; i < _shop.Count; i++) Assert.IsTrue(_shop.Get(i).CanBuy);
            _changes = 0;

            _run.Stop();

            Assert.GreaterOrEqual(_changes, 1);
            for (var i = 0; i < _shop.Count; i++) Assert.IsFalse(_shop.Get(i).CanBuy);
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
        public void The_six_items_stay_the_same_through_the_run_and_keep_their_levels()
        {
            EnterBattle("knight", "archer", "mage");
            _wallet.Coins = 1000;
            _shop.Buy(1);

            _run.SetStep(RunStep.Paused);
            _run.SetStep(RunStep.Playing);
            _wallet.Grant(50);
            _run.CountClearedWave(1);

            Assert.AreEqual(6, _shop.Count);
            Assert.AreEqual("knight", _shop.Get(0).HeroCode);
            Assert.AreEqual("archer", _shop.Get(1).HeroCode);
            Assert.AreEqual("mage", _shop.Get(2).HeroCode);
            Assert.AreEqual(1, _shop.Get(0).Level);
            Assert.AreEqual(2, _shop.Get(1).Level);
        }

        [Test]
        public void A_team_of_the_wrong_size_builds_no_items()
        {
            Assert.Throws<InvalidOperationException>(() => EnterBattle("knight", "archer"));

            Assert.AreEqual(0, _shop.Count);
        }

        [Test]
        public void Buying_works_again_after_resuming_from_pause()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            _run.SetStep(RunStep.Paused);
            _run.SetStep(RunStep.Playing);

            Assert.IsTrue(_shop.Get(0).CanBuy);
            Assert.AreEqual(PurchaseResult.Bought, _shop.Buy(0));
        }

        [Test]
        public void Buying_stays_open_the_whole_battle_including_the_break_between_waves()
        {
            // The break between waves raises no run event: the shop closes only on pause or at the end of the run.
            EnterBattle();
            _wallet.Coins = 1000;

            Assert.AreEqual(PurchaseResult.Bought, _shop.Buy(0));
            _run.CountClearedWave(1);
            Assert.IsTrue(_shop.Get(3).CanBuy);
            Assert.AreEqual(PurchaseResult.Bought, _shop.Buy(3));
            _run.CountClearedWave(2);
            Assert.AreEqual(PurchaseResult.Bought, _shop.Buy(5));
        }

        [Test]
        public void Every_item_keeps_selling_levels_with_no_cap_and_no_refund()
        {
            EnterBattle();
            _wallet.Coins = int.MaxValue;
            long paid = 0;

            for (var i = 0; i < 30; i++)
            {
                var price = _shop.Get(0).NextPrice;
                Assert.AreEqual(PurchaseResult.Bought, _shop.Buy(0));
                paid += price;
            }

            Assert.AreEqual(31, _shop.Get(0).Level);
            Assert.AreEqual(int.MaxValue - paid, (long)_wallet.Coins);
        }

        [Test]
        public void A_buy_made_before_the_castle_falls_stays_and_later_taps_are_refused()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            _shop.Buy(3);

            _run.Stop();

            Assert.AreEqual(PurchaseResult.Unavailable, _shop.Buy(3));
            Assert.AreEqual(2, _shop.Get(3).Level);
            Assert.AreEqual(960, _wallet.Coins);
            CollectionAssert.AreEqual(new[] { (CastleUpgrade.Health, 2) }, _castle.Applied);
        }

        [Test]
        public void Coins_that_arrive_before_a_tap_count_for_that_tap()
        {
            EnterBattle();
            _wallet.Coins = 20;
            Assert.AreEqual(PurchaseResult.NotEnoughCoins, _shop.Buy(0));

            _wallet.Grant(10);

            Assert.AreEqual(PurchaseResult.Bought, _shop.Buy(0));
            Assert.AreEqual(0, _wallet.Coins);
        }

        [Test]
        public void Hero_price_from_level_30_to_31_is_the_whole_number_180635()
        {
            Assert.AreEqual(180635, new PriceCurve(30, 1.35).PriceAt(30));
        }

        [Test]
        public void Several_buys_of_one_item_report_every_level_one_by_one_in_order()
        {
            EnterBattle();
            _wallet.Coins = 10000;

            _shop.Buy(4);
            _shop.Buy(4);
            _shop.Buy(4);

            CollectionAssert.AreEqual(
                new[] { (CastleUpgrade.Attack, 2), (CastleUpgrade.Attack, 3), (CastleUpgrade.Attack, 4) },
                _castle.Applied);
            Assert.IsEmpty(_heroes.Applied);
        }

        [Test]
        public void A_level_report_reaches_the_owner_before_the_buy_returns()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            var reportedBeforeBarUpdate = false;
            _shop.ItemsChanged += () => reportedBeforeBarUpdate = _heroes.Applied.Count == 1;

            _shop.Buy(1);

            Assert.AreEqual(1, _heroes.Applied.Count);
            Assert.IsTrue(reportedBeforeBarUpdate);
        }

        [Test]
        public void Refused_buys_report_nothing_and_never_repeat_an_old_level()
        {
            EnterBattle();
            _wallet.Coins = 30;
            _shop.Buy(0);

            _shop.Buy(0);                         // not enough coins
            _wallet.Coins = 1000;
            _run.SetStep(RunStep.Paused);
            _shop.Buy(0);                         // paused
            _run.SetStep(RunStep.Playing);
            _shop.Buy(0);
            _run.Stop();
            _shop.Buy(0);                         // run over

            CollectionAssert.AreEqual(new[] { (0, 2), (0, 3) }, _heroes.Applied);
            Assert.IsEmpty(_castle.Applied);
        }

        [Test]
        public void A_new_run_does_not_report_level_1_to_anyone()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            _shop.Buy(0);
            _shop.Buy(3);
            _run.Stop();
            _heroes.Applied.Clear();
            _castle.Applied.Clear();

            EnterBattle("mage", "priest", "archer");

            Assert.IsEmpty(_heroes.Applied);
            Assert.IsEmpty(_castle.Applied);
        }

        [Test]
        public void The_session_follows_the_run_only_between_run_and_dispose()
        {
            var shop = new UpgradeShopLogic(StandardPrices(), _run, _wallet, _wallet, _heroes, _castle, _clock);
            var session = new UpgradeSession(shop);

            session.Run();
            EnterBattle();
            Assert.AreEqual(6, shop.Count);

            session.DisposeAsync().GetAwaiter().GetResult();
            _run.Stop();
            EnterBattle("mage", "priest", "archer");
            Assert.AreEqual("knight", shop.Get(0).HeroCode);
        }

        [Test]
        public void After_detach_the_shop_no_longer_follows_the_run()
        {
            _shop.Detach();

            EnterBattle();

            Assert.AreEqual(0, _shop.Count);
        }

        // ---- Taps through the shared clock's queue (feature-sim-clock clock-values#input-next-step) -----------

        [Test]
        public void A_buy_tap_pays_nothing_until_the_next_clock_step()
        {
            EnterBattle();
            _wallet.Coins = 100;
            var applied = new List<(int index, PurchaseResult result)>();
            _shop.OrderApplied += (index, result) => applied.Add((index, result));

            _shop.Order(0);

            Assert.AreEqual(100, _wallet.Coins);
            Assert.AreEqual(1, _shop.Get(0).Level);
            Assert.IsEmpty(_heroes.Applied);
            Assert.IsEmpty(applied);
            Assert.AreEqual(UpgradeShopLogic.ClockOwner, _clock.Owners[0]);

            _clock.Step();

            Assert.AreEqual(70, _wallet.Coins);
            Assert.AreEqual(2, _shop.Get(0).Level);
            CollectionAssert.AreEqual(new[] { (0, 2) }, _heroes.Applied);
            CollectionAssert.AreEqual(new[] { (0, PurchaseResult.Bought) }, applied);
        }

        [Test]
        public void Two_buy_taps_before_a_step_are_applied_in_the_order_tapped()
        {
            EnterBattle();
            _wallet.Coins = 50;
            var applied = new List<(int index, PurchaseResult result)>();
            _shop.OrderApplied += (index, result) => applied.Add((index, result));

            _shop.Order(3);   // castle health, 40: bought
            _shop.Order(0);   // hero slot 1, 30: only 10 left

            _clock.Step();

            CollectionAssert.AreEqual(
                new[] { (3, PurchaseResult.Bought), (0, PurchaseResult.NotEnoughCoins) }, applied);
            Assert.AreEqual(10, _wallet.Coins);
            Assert.AreEqual(2, _shop.Get(3).Level);
            Assert.AreEqual(1, _shop.Get(0).Level);
        }

        [Test]
        public void A_buy_tap_followed_by_a_pause_before_the_step_is_refused()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            var applied = new List<(int index, PurchaseResult result)>();
            _shop.OrderApplied += (index, result) => applied.Add((index, result));

            _shop.Order(0);
            _run.SetStep(RunStep.Paused);
            _clock.Step();

            CollectionAssert.AreEqual(new[] { (0, PurchaseResult.Unavailable) }, applied);
            Assert.AreEqual(1000, _wallet.Coins);
            Assert.AreEqual(1, _shop.Get(0).Level);
            Assert.IsEmpty(_heroes.Applied);
        }

        [Test]
        public void A_buy_tap_followed_by_the_castle_falling_before_the_step_is_refused()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            var applied = new List<(int index, PurchaseResult result)>();
            _shop.OrderApplied += (index, result) => applied.Add((index, result));

            _shop.Order(3);
            _run.Stop();
            _clock.Step();

            CollectionAssert.AreEqual(new[] { (3, PurchaseResult.Unavailable) }, applied);
            Assert.AreEqual(1000, _wallet.Coins);
            Assert.IsEmpty(_castle.Applied);
        }

        [Test]
        public void A_buy_tap_is_lost_when_the_run_is_cleared_before_the_step()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            var applied = 0;
            _shop.OrderApplied += (index, result) => applied++;

            _shop.Order(0);
            _clock.ClearRun();
            _clock.Step();

            Assert.AreEqual(0, applied);
            Assert.AreEqual(1000, _wallet.Coins);
            Assert.AreEqual(1, _shop.Get(0).Level);
        }

        [Test]
        public void A_buy_tap_left_from_an_earlier_run_does_not_buy_in_the_new_run()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            var applied = 0;
            _shop.OrderApplied += (index, result) => applied++;

            _shop.Order(0);
            _run.Stop();
            EnterBattle("mage", "priest", "archer");
            _clock.Step();

            Assert.AreEqual(0, applied);
            Assert.AreEqual(1000, _wallet.Coins);
            Assert.AreEqual(1, _shop.Get(0).Level);
        }

        [Test]
        public void A_buy_tap_with_a_wrong_index_is_refused_at_the_step()
        {
            EnterBattle();
            _wallet.Coins = 1000;
            var applied = new List<(int index, PurchaseResult result)>();
            _shop.OrderApplied += (index, result) => applied.Add((index, result));

            _shop.Order(6);
            _clock.Step();

            CollectionAssert.AreEqual(new[] { (6, PurchaseResult.Unavailable) }, applied);
            Assert.AreEqual(1000, _wallet.Coins);
        }

        // ---- Fakes of the other features -------------------------------------------------------------------

        private sealed class FakeClock : ISimClock
        {
            private readonly List<Action> _queue = new List<Action>();
            public readonly List<string> Owners = new List<string>();

            public SimMode Mode => SimMode.FixedTick;
            public float StepSeconds => 0.025f;
            public long StepCount { get; private set; }
            public long GameplayStepCount => StepCount;
            public double GameplaySeconds => StepCount * 0.025;

            public IDisposable OnStep(string owner, Action<SimStep> onStep) =>
                throw new NotSupportedException("Upgrade has no per-step logic.");

            public void Enqueue(string owner, Action apply)
            {
                Owners.Add(owner);
                _queue.Add(apply);
            }

            /// <summary>Starts one step: applies the queued actions in the order queued.</summary>
            public void Step()
            {
                StepCount++;
                var due = _queue.ToArray();
                _queue.Clear();
                foreach (var apply in due) apply();
            }

            /// <summary>Like the real clock's run clear: drops the queue.</summary>
            public void ClearRun() => _queue.Clear();
        }

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
