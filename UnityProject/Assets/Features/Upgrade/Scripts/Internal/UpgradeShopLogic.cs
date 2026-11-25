using System;
using System.Collections.Generic;
using Game.Castle;
using Game.Hero;
using Game.Run;
using Game.SimClock;
using Game.Wallet;

namespace Game.Upgrade
{
    /// <summary>
    /// The upgrade shop of a run: six items (heroes by slot, then castle health, attack, skills), each at
    /// level 1 when the run starts. Buying pays through the wallet and pushes the new level to the hero or castle
    /// at once. Buying works only while the battle is running and not paused.
    /// A player tap goes through <see cref="Order"/>: it waits in the shared clock's queue and is bought at the start
    /// of the next step, checked again by the same rules then (feature-sim-clock clock-values#input-next-step).
    /// </summary>
    internal sealed class UpgradeShopLogic : IUpgradeShop, IUpgradeOrders
    {
        /// <summary>Owner name of this feature in the shared clock.</summary>
        internal const string ClockOwner = "Upgrade";

        private readonly UpgradePrices _prices;
        private readonly IRunFlow _run;
        private readonly IWalletService _wallet;
        private readonly IWalletBalance _balance;
        private readonly IHeroSquad _heroes;
        private readonly ICastleUpgrades _castle;
        private readonly ISimClock _clock;
        private readonly List<Entry> _items = new List<Entry>();

        private bool _battleOn;
        private bool _attached;
        private int _runNumber;

        public UpgradeShopLogic(UpgradePrices prices, IRunFlow run, IWalletService wallet, IWalletBalance balance,
            IHeroSquad heroes, ICastleUpgrades castle, ISimClock clock)
        {
            _prices = prices;
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _heroes = heroes ?? throw new ArgumentNullException(nameof(heroes));
            _castle = castle ?? throw new ArgumentNullException(nameof(castle));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public event Action ItemsChanged;

        public event Action<int, PurchaseResult> OrderApplied;

        /// <summary>
        /// Queues one level of the item at <paramref name="index"/>. Nothing is paid until the next step; then the
        /// order is bought by the usual rules (paused, battle over, coins) and <see cref="OrderApplied"/> tells the outcome.
        /// An order left over from an earlier run is dropped without a report.
        /// </summary>
        public void Order(int index)
        {
            var runNumber = _runNumber;
            _clock.Enqueue(ClockOwner, () =>
            {
                if (runNumber != _runNumber) return;
                var result = Buy(index);
                OrderApplied?.Invoke(index, result);
            });
        }

        public int Count => _items.Count;

        private bool CanBuyNow => _battleOn && _run.Step == RunStep.Playing && !_run.IsPaused;

        /// <summary>Starts listening to the run and the wallet. Call once; undo with <see cref="Detach"/>.</summary>
        public void Attach()
        {
            if (_attached) return;
            _attached = true;
            _run.RunStarted += OnRunStarted;
            _run.BattleStarted += OnBattleStarted;
            _run.BattleStopped += OnBattleStopped;
            _run.BattleCleared += OnBattleStopped;
            _run.StepChanged += OnStepChanged;
            _balance.CoinsChanged += OnCoinsChanged;
        }

        public void Detach()
        {
            if (!_attached) return;
            _attached = false;
            _run.RunStarted -= OnRunStarted;
            _run.BattleStarted -= OnBattleStarted;
            _run.BattleStopped -= OnBattleStopped;
            _run.BattleCleared -= OnBattleStopped;
            _run.StepChanged -= OnStepChanged;
            _balance.CoinsChanged -= OnCoinsChanged;
        }

        public UpgradeItem Get(int index)
        {
            if (index < 0 || index >= _items.Count) throw new ArgumentOutOfRangeException(nameof(index));
            var entry = _items[index];
            var price = _prices.For(entry.Kind).PriceAt(entry.Level);
            return new UpgradeItem(entry.Kind, entry.HeroSlot, entry.HeroCode, entry.Level, price,
                CanBuyNow && _balance.Coins >= price);
        }

        public PurchaseResult Buy(int index)
        {
            if (!CanBuyNow || index < 0 || index >= _items.Count) return PurchaseResult.Unavailable;

            var entry = _items[index];
            var price = _prices.For(entry.Kind).PriceAt(entry.Level);
            var spent = _wallet.TrySpend(price);
            if (spent == SpendResult.NotEnoughCoins) return PurchaseResult.NotEnoughCoins;
            if (spent != SpendResult.Ok) return PurchaseResult.Unavailable;

            entry.Level++;
            _items[index] = entry;
            ApplyLevel(entry);
            ItemsChanged?.Invoke();
            return PurchaseResult.Bought;
        }

        /// <summary>Builds the six items for <paramref name="team"/>, every one at level 1.</summary>
        internal void StartRun(RunTeam team)
        {
            if (team.Count != _prices.TeamSize)
                throw new InvalidOperationException(
                    $"Upgrade: the run brought {team.Count} heroes, expected {_prices.TeamSize}.");

            _runNumber++;
            _items.Clear();
            for (var slot = 0; slot < team.Count; slot++)
                _items.Add(new Entry(UpgradeKind.Hero, slot, team.HeroCodes[slot]));
            _items.Add(new Entry(UpgradeKind.CastleHealth, -1, null));
            _items.Add(new Entry(UpgradeKind.CastleAttack, -1, null));
            _items.Add(new Entry(UpgradeKind.CastleSkills, -1, null));

            _battleOn = false;
            ItemsChanged?.Invoke();
        }

        private void ApplyLevel(Entry entry)
        {
            switch (entry.Kind)
            {
                case UpgradeKind.Hero:
                    _heroes.ApplyLevel(entry.HeroSlot, entry.Level);
                    break;
                case UpgradeKind.CastleHealth:
                    _castle.ApplyLevel(CastleUpgrade.Health, entry.Level);
                    break;
                case UpgradeKind.CastleAttack:
                    _castle.ApplyLevel(CastleUpgrade.Attack, entry.Level);
                    break;
                case UpgradeKind.CastleSkills:
                    _castle.ApplyLevel(CastleUpgrade.Skills, entry.Level);
                    break;
            }
        }

        private void OnRunStarted(RunTeam team) => StartRun(team);

        private void OnBattleStarted()
        {
            _battleOn = true;
            ItemsChanged?.Invoke();
        }

        private void OnBattleStopped()
        {
            if (!_battleOn) return;
            _battleOn = false;
            ItemsChanged?.Invoke();
        }

        private void OnStepChanged(RunStep step) => ItemsChanged?.Invoke();

        private void OnCoinsChanged(int coins) => ItemsChanged?.Invoke();

        private struct Entry
        {
            public Entry(UpgradeKind kind, int heroSlot, string heroCode)
            {
                Kind = kind;
                HeroSlot = heroSlot;
                HeroCode = heroCode;
                Level = 1;
            }

            public UpgradeKind Kind;
            public int HeroSlot;
            public string HeroCode;
            public int Level;
        }
    }
}
