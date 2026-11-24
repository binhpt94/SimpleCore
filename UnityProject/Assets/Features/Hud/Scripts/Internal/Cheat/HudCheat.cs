#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Game.Castle;
using Game.Run;
using Game.Wallet;
using Game.Wave;

namespace Game.Hud
{
    /// <summary>
    /// Dev-only state of feature-hud/cheat: "mid-battle" (castle 300/1000, 245 coins, wave 7 just started,
    /// Frost cooling with 12 of 35 s left, Thunder and Rally ready). Every value goes through its owner's own
    /// contract; the HUD then shows it as usual. Skills the battle does not have are reported, not faked.
    /// </summary>
    internal sealed class HudCheat
    {
        public const string MidBattle = "mid-battle";
        public const int MidBattleWave = 7;
        public const float MidBattleHealth = 300f;
        public const int MidBattleCoins = 245;
        public const float MidBattleFrostLeft = 12f;

        private readonly IRunFlow _run;
        private readonly IWalletBalance _balance;
        private readonly IWalletIncome _income;
        private readonly IWalletService _spend;
        private readonly ICastleSkills _skills;
        private readonly ICastleCheat _castle;
        private readonly IWaveCheat _waves;

        public HudCheat(IRunFlow run, IWalletBalance balance, IWalletIncome income, IWalletService spend,
            ICastleSkills skills, ICastleCheat castle, IWaveCheat waves)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _income = income ?? throw new ArgumentNullException(nameof(income));
            _spend = spend ?? throw new ArgumentNullException(nameof(spend));
            _skills = skills ?? throw new ArgumentNullException(nameof(skills));
            _castle = castle ?? throw new ArgumentNullException(nameof(castle));
            _waves = waves ?? throw new ArgumentNullException(nameof(waves));
        }

        /// <summary>
        /// Applies "mid-battle" while the battle is being played: wave first (its start must not undo the rest),
        /// then castle health and cooldowns, then coins. Returns the parts it could not set (empty when the whole
        /// state was applied); nothing changes outside a running battle.
        /// </summary>
        public IReadOnlyList<string> ApplyMidBattle()
        {
            var missing = new List<string>();
            if (_run.Step != RunStep.Playing)
            {
                missing.Add("battle not running");
                return missing;
            }

            _waves.StartWave(MidBattleWave);
            _castle.SetHealth(MidBattleHealth);

            SetCooldown(CastleSkill.Thunder, 0f, missing);
            SetCooldown(CastleSkill.Frost, MidBattleFrostLeft, missing);
            SetCooldown(CastleSkill.Rally, 0f, missing);

            var difference = MidBattleCoins - _balance.Coins;
            if (difference > 0) _income.Add(difference);
            else if (difference < 0) _spend.TrySpend(-difference);

            return missing;
        }

        private void SetCooldown(CastleSkill skill, float secondsLeft, List<string> missing)
        {
            if (Has(skill)) _castle.SetCooldownLeft(skill, secondsLeft);
            else missing.Add(skill + ": không có ở mức này");
        }

        private bool Has(CastleSkill skill)
        {
            foreach (var present in _skills.Skills)
                if (present == skill) return true;
            return false;
        }
    }
}
#endif
