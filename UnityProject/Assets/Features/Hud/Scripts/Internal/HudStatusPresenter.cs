using System;
using Game.Castle;
using Game.Run;
using Game.Wallet;
using Game.Wave;

namespace Game.Hud
{
    /// <summary>
    /// Castle health, coins, wave number and the "Đợt N" banner (feature-hud/hud-status/status-spec).
    /// Every number comes from its owner the moment it changes; the HUD keeps none of its own.
    /// </summary>
    internal sealed class HudStatusPresenter
    {
        private readonly IHudView _view;
        private readonly ICastle _castle;
        private readonly IWalletBalance _wallet;
        private readonly IWaveProgress _waves;
        private readonly IRunFlow _run;
        private readonly float _bannerSeconds;
        private float _bannerLeft;
        private bool _started;

        public HudStatusPresenter(IHudView view, ICastle castle, IWalletBalance wallet, IWaveProgress waves, IRunFlow run, HudTimings timings)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _castle = castle ?? throw new ArgumentNullException(nameof(castle));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _waves = waves ?? throw new ArgumentNullException(nameof(waves));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _bannerSeconds = timings.BannerSeconds;
        }

        public bool IsBannerShown => _bannerLeft > 0f;

        public void Start()
        {
            if (_started) return;
            _started = true;
            _castle.HealthChanged += OnHealthChanged;
            _wallet.CoinsChanged += OnCoinsChanged;
            _waves.CurrentWaveChanged += OnWaveChanged;
            _waves.WaveStarted += OnWaveStarted;
            _run.RunStarted += OnRunStarted;
            _run.BattleStopped += HideBanner;
            _run.BattleCleared += HideBanner;

            OnHealthChanged();
            OnCoinsChanged(_wallet.Coins);
            OnWaveChanged(_waves.CurrentWave);
            HideBanner();
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _castle.HealthChanged -= OnHealthChanged;
            _wallet.CoinsChanged -= OnCoinsChanged;
            _waves.CurrentWaveChanged -= OnWaveChanged;
            _waves.WaveStarted -= OnWaveStarted;
            _run.RunStarted -= OnRunStarted;
            _run.BattleStopped -= HideBanner;
            _run.BattleCleared -= HideBanner;
        }

        /// <summary>Counts the banner down; stands still while the battle is paused.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!_started || _bannerLeft <= 0f || _run.IsPaused || deltaSeconds <= 0f) return;
            _bannerLeft -= deltaSeconds;
            if (_bannerLeft <= 0f) HideBanner();
        }

        private void OnHealthChanged()
        {
            _view.ShowHealth(HudText.Health(_castle.Health, _castle.MaxHealth), HudText.HealthFill(_castle.Health, _castle.MaxHealth));
        }

        private void OnCoinsChanged(int coins) => _view.ShowCoins(HudText.Coins(coins));

        private void OnWaveChanged(int wave) => _view.ShowWave(HudText.Wave(wave));

        private void OnWaveStarted(int wave)
        {
            if (_bannerSeconds <= 0f) return;
            _bannerLeft = _bannerSeconds;
            _view.ShowBanner(HudText.Wave(wave));
        }

        private void OnRunStarted(RunTeam team) => HideBanner();

        private void HideBanner()
        {
            _bannerLeft = 0f;
            _view.HideBanner();
        }
    }
}
