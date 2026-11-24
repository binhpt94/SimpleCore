using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Hero;
using Game.Run;
using Game.Upgrade;
using Game.Wallet;

namespace Game.HudUpgrade.Tests
{
    internal sealed class FakeWallet : IWalletBalance, IWalletIncome, IWalletService
    {
        public int Coins { get; set; }
        public event Action<int> CoinsChanged;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            CoinsChanged?.Invoke(Coins);
        }

        public SpendResult TrySpend(int amount)
        {
            if (amount < 0) return SpendResult.InvalidAmount;
            if (amount > Coins) return SpendResult.NotEnoughCoins;
            Coins -= amount;
            CoinsChanged?.Invoke(Coins);
            return SpendResult.Ok;
        }
    }

    /// <summary>Shop with simple prices (base + 10 per level) that pays from a <see cref="FakeWallet"/>.</summary>
    internal sealed class FakeShop : IUpgradeShop
    {
        private readonly FakeWallet _wallet;
        private readonly List<(UpgradeKind kind, int slot, string code, int level, int basePrice)> _items =
            new List<(UpgradeKind, int, string, int, int)>();

        public FakeShop(FakeWallet wallet)
        {
            _wallet = wallet;
            _wallet.CoinsChanged += _ => Changed();
        }

        public bool Open { get; set; } = true;

        /// <summary>Buy refuses although Get still says CanBuy (the state changed between the read and the tap).</summary>
        public bool RejectBuys { get; set; }
        public int BuyCalls { get; private set; }
        public event Action ItemsChanged;

        public int Count => _items.Count;

        public void SetTeam(params string[] heroCodes)
        {
            _items.Clear();
            for (var i = 0; i < heroCodes.Length; i++) _items.Add((UpgradeKind.Hero, i, heroCodes[i], 1, 30));
            _items.Add((UpgradeKind.CastleHealth, -1, null, 1, 40));
            _items.Add((UpgradeKind.CastleAttack, -1, null, 1, 40));
            _items.Add((UpgradeKind.CastleSkills, -1, null, 1, 60));
            Changed();
        }

        public void SetPrice(int index, int basePrice)
        {
            var it = _items[index];
            _items[index] = (it.kind, it.slot, it.code, it.level, basePrice);
            Changed();
        }

        public void Changed() => ItemsChanged?.Invoke();

        public UpgradeItem Get(int index)
        {
            var it = _items[index];
            var price = Price(index);
            return new UpgradeItem(it.kind, it.slot, it.code, it.level, price, Open && _wallet.Coins >= price);
        }

        public PurchaseResult Buy(int index)
        {
            BuyCalls++;
            if (RejectBuys || !Open || index < 0 || index >= _items.Count) return PurchaseResult.Unavailable;
            var price = Price(index);
            if (_wallet.Coins < price) return PurchaseResult.NotEnoughCoins;
            _wallet.TrySpend(price);
            var it = _items[index];
            _items[index] = (it.kind, it.slot, it.code, it.level + 1, it.basePrice);
            Changed();
            return PurchaseResult.Bought;
        }

        private int Price(int index) => _items[index].basePrice + 10 * (_items[index].level - 1);
    }

    internal sealed class FakeRoster : IHeroRoster
    {
        private readonly List<HeroInfo> _heroes = new List<HeroInfo>
        {
            new HeroInfo("knight", "Hiệp sĩ", HeroGroup.Melee, HeroAttack.SingleTarget),
            new HeroInfo("archer", "Cung thủ", HeroGroup.Ranged, HeroAttack.SingleTarget),
            new HeroInfo("mage", "Pháp sư", HeroGroup.Ranged, HeroAttack.Blast),
            new HeroInfo("axeman", "Đấu sĩ rìu", HeroGroup.Melee, HeroAttack.FrontCleave),
        };

        public IReadOnlyList<HeroInfo> Heroes => _heroes;

        public bool TryGet(string code, out HeroInfo hero)
        {
            foreach (var h in _heroes)
                if (h.Code == code)
                {
                    hero = h;
                    return true;
                }

            hero = default;
            return false;
        }
    }

    internal sealed class FakeRun : IRunFlow
    {
        public RunStep Step { get; private set; } = RunStep.Home;
        public event Action<RunStep> StepChanged;
        public RunTeam Team => default;
        public event Action<RunTeam> RunStarted;
        public event Action BattleStarted;
        public event Action BattleStopped;
        public event Action BattleCleared;
        public bool IsPaused => Step == RunStep.Paused;

        public void GoTo(RunStep step)
        {
            Step = step;
            StepChanged?.Invoke(step);
        }

        /// <summary>Step changes but StepChanged has not been raised yet (another listener runs first).</summary>
        public void SetStepQuietly(RunStep step) => Step = step;

        public void StartBattle() => BattleStarted?.Invoke();

        public int BattleStartedListeners => BattleStarted?.GetInvocationList().Length ?? 0;

        public void Play() { }
        public void EnterBattle(RunTeam team) { }
        public void Pause() { }
        public void Resume() { }
        public void Abandon() { }
        public void Replay() { }
        public void GoHome() { }
        public void Lose() { }
        public void CountClearedWave(int wave) { }

        // Keep the unused events referenced so the compiler does not warn.
        internal void Unused() { RunStarted?.Invoke(default); BattleStopped?.Invoke(); BattleCleared?.Invoke(); }
    }

    internal sealed class FakeBarView : IUpgradeBarView
    {
        public readonly List<ButtonPicture> Pictures = new List<ButtonPicture>();
        public readonly Dictionary<int, ButtonLook> Looks = new Dictionary<int, ButtonLook>();
        public readonly List<int> Bounces = new List<int>();
        public int Builds;
        public bool Visible;
        public Action<int> OnTap;

        public void Build(IReadOnlyList<ButtonPicture> buttons, Action<int> onTap)
        {
            Builds++;
            Pictures.Clear();
            Pictures.AddRange(buttons);
            Looks.Clear();
            OnTap = onTap;
        }

        public void SetButton(int index, ButtonLook look) => Looks[index] = look;
        public void Bounce(int index) => Bounces.Add(index);
        public void SetVisible(bool visible) => Visible = visible;
        public void Tap(int index) => OnTap(index);
    }

    internal sealed class FakeViewFactory : IUpgradeBarViewFactory
    {
        public readonly FakeBarView View = new FakeBarView();
        public int Created;
        public int Destroyed;

        public IUpgradeBarView Create()
        {
            Created++;
            return View;
        }

        public void Destroy(IUpgradeBarView view) => Destroyed++;
    }

    internal sealed class FakeSession : ISessionScene
    {
        public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
        public SessionState State => SessionState.Init;
        public event Action<SessionState> StateChanged;
        public void Join(ISessionParticipant participant) => Joined.Add(participant);
        public UniTask EndAsync() => UniTask.CompletedTask;
        internal void Unused() => StateChanged?.Invoke(State);
    }

    internal sealed class FakeRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<T>(T service) where T : class => _services[typeof(T)] = service;

        public T Resolve<T>() where T : class =>
            _services.TryGetValue(typeof(T), out var s) ? (T)s : throw new InvalidOperationException("missing " + typeof(T).Name);

        public bool TryResolve<T>(out T service) where T : class
        {
            var found = _services.TryGetValue(typeof(T), out var s);
            service = found ? (T)s : null;
            return found;
        }
    }
}
