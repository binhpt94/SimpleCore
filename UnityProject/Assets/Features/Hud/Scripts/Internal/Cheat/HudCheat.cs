#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Game.Run;
using Game.Wallet;

namespace Game.Hud
{
    /// <summary>
    /// Dev-only state of feature-hud/cheat: "mid-battle" (castle 300/1000, 245 coins, wave 7 just started,
    /// Frost cooling with 12 of 35 s left, Thunder and Rally ready). Every value goes through its owner's own
    /// contract; the HUD then shows it as usual. Parts the owners offer no way to set yet are reported, not faked.
    /// </summary>
    internal sealed class HudCheat
    {
        public const string MidBattle = "mid-battle";
        public const int MidBattleCoins = 245;

        private readonly IRunFlow _run;
        private readonly IWalletBalance _balance;
        private readonly IWalletIncome _income;
        private readonly IWalletService _spend;

        public HudCheat(IRunFlow run, IWalletBalance balance, IWalletIncome income, IWalletService spend)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _income = income ?? throw new ArgumentNullException(nameof(income));
            _spend = spend ?? throw new ArgumentNullException(nameof(spend));
        }

        /// <summary>
        /// Applies "mid-battle" while the battle is being played. Returns the parts it could not set
        /// (empty when the whole state was applied); nothing changes outside a running battle.
        /// </summary>
        public IReadOnlyList<string> ApplyMidBattle()
        {
            var missing = new List<string>();
            if (_run.Step != RunStep.Playing)
            {
                missing.Add("battle not running");
                return missing;
            }

            var difference = MidBattleCoins - _balance.Coins;
            if (difference > 0) _income.Add(difference);
            else if (difference < 0) _spend.TrySpend(-difference);

            // No contract sets these yet (Castle, Wave): see the Hud report to CodeLead.
            missing.Add("castle health 300/1000 (Castle)");
            missing.Add("wave 7 just started (Wave)");
            missing.Add("Frost cooling 12/35 s (Castle)");
            return missing;
        }
    }
}
#endif
