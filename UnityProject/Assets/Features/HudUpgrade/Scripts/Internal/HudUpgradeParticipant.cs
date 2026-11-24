using System;
using Game.Core;
using Game.Hero;
using Game.Run;
using Game.Upgrade;
using Game.Wallet;

namespace Game.HudUpgrade
{
    /// <summary>The upgrade bar's part in the scene session: opens the bar on Run, removes it when the session ends.</summary>
    internal sealed class HudUpgradeParticipant : SessionParticipantBase
    {
        private readonly IUpgradeShop _shop;
        private readonly IHeroRoster _roster;
        private readonly IRunFlow _run;
        private readonly IUpgradeBarViewFactory _views;
        private readonly CheatSetup _cheat;

        public HudUpgradeParticipant(IUpgradeShop shop, IHeroRoster roster, IRunFlow run, IUpgradeBarViewFactory views,
            CheatSetup cheat = default)
        {
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _views = views ?? throw new ArgumentNullException(nameof(views));
            _cheat = cheat;
        }

        protected override void OnRun()
        {
            var view = _views.Create();
            var presenter = new UpgradeBarPresenter(_shop, _roster, _run, view);
            WhenDisposed(() => _views.Destroy(view));
            WhenDisposed(presenter.Dispose);
            presenter.Start();
            StartCheat();
        }

        private void StartCheat()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_cheat.State != HudUpgradeCheatState.MixedButtons) return;
            var cheat = new UpgradeBarCheat(_shop, _cheat.Balance, _cheat.Income, _cheat.Wallet);
            Action apply = () =>
            {
                var mismatch = cheat.FindTeamMismatch(_cheat.Team);
                if (mismatch != null) UnityEngine.Debug.LogWarning("HudUpgrade cheat mixed-buttons: " + mismatch);
                if (!cheat.Apply(_cheat.Coins, _cheat.Levels))
                    UnityEngine.Debug.LogWarning("HudUpgrade cheat mixed-buttons: cửa hàng từ chối mua, state chưa dựng đủ.");
            };
            _run.BattleStarted += apply;
            WhenDisposed(() => _run.BattleStarted -= apply);
#endif
        }

        /// <summary>What the dev cheat needs. Default (no state) when the cheat is off.</summary>
        internal readonly struct CheatSetup
        {
            public CheatSetup(HudUpgradeCheatState state, int coins, int[] levels,
                IWalletBalance balance, IWalletIncome income, IWalletService wallet, string[] team = null)
            {
                State = state;
                Coins = coins;
                Levels = levels ?? Array.Empty<int>();
                Team = team ?? Array.Empty<string>();
                Balance = balance;
                Income = income;
                Wallet = wallet;
            }

            public HudUpgradeCheatState State { get; }
            public int Coins { get; }
            public int[] Levels { get; }
            public string[] Team { get; }
            public IWalletBalance Balance { get; }
            public IWalletIncome Income { get; }
            public IWalletService Wallet { get; }
        }
    }
}
