using System;
using System.Collections.Generic;

namespace Game.Hero
{
    /// <summary>Keeps one display per hero of the squad and pushes the squad's state and events down to it.</summary>
    internal sealed class HeroPresenter : IDisposable
    {
        private readonly HeroSquad _squad;
        private readonly HeroConfig _config;
        private readonly IHeroDisplayFactory _factory;
        private readonly Dictionary<HeroUnit, IHeroDisplay> _displays = new Dictionary<HeroUnit, IHeroDisplay>();
        private bool _attached;

        public HeroPresenter(HeroSquad squad, HeroConfig config, IHeroDisplayFactory factory)
        {
            _squad = squad ?? throw new ArgumentNullException(nameof(squad));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public int DisplayCount => _displays.Count;

        public void Attach()
        {
            if (_attached) return;
            _attached = true;
            _squad.TeamChanged += Rebuild;
            _squad.Struck += OnStruck;
            _squad.Hurt += OnHurt;
            _squad.Fell += OnFell;
            _squad.Respawned += OnRespawned;
            _squad.LevelledUp += OnLevelledUp;
            Rebuild();
        }

        /// <summary>Pushes positions and moves animations on by <paramref name="deltaSeconds"/> (0 outside the battle).</summary>
        public void Refresh(float deltaSeconds)
        {
            foreach (var pair in _displays)
            {
                var unit = pair.Key;
                pair.Value.SetPose(unit.X, unit.Facing, unit.IsMoving);
                pair.Value.Animate(deltaSeconds);
            }
        }

        public void Dispose()
        {
            if (!_attached) return;
            _attached = false;
            _squad.TeamChanged -= Rebuild;
            _squad.Struck -= OnStruck;
            _squad.Hurt -= OnHurt;
            _squad.Fell -= OnFell;
            _squad.Respawned -= OnRespawned;
            _squad.LevelledUp -= OnLevelledUp;
            RemoveAll();
        }

        private void Rebuild()
        {
            RemoveAll();
            foreach (var unit in _squad.Units)
            {
                var display = _factory.Create();
                var hero = unit.Hero;
                display.Setup(hero.code, hero.sprite, hero.placeholderColor, _config.BoxWidth, _config.BoxHeight);
                display.SetPose(unit.X, unit.Facing, unit.IsMoving);
                _displays.Add(unit, display);
            }
        }

        private void RemoveAll()
        {
            foreach (var display in _displays.Values) display.Remove();
            _displays.Clear();
            _factory.RemoveAll();
        }

        private void OnStruck(HeroUnit unit, HeroStrike strike)
        {
            if (_displays.TryGetValue(unit, out var display)) display.PlayStrike(strike);
        }

        private void OnHurt(HeroUnit unit)
        {
            if (_displays.TryGetValue(unit, out var display)) display.PlayHurt();
        }

        private void OnFell(HeroUnit unit)
        {
            if (_displays.TryGetValue(unit, out var display)) display.PlayFall(_config.FallSeconds);
        }

        private void OnRespawned(HeroUnit unit)
        {
            if (!_displays.TryGetValue(unit, out var display)) return;
            display.SetPose(unit.X, unit.Facing, false);
            display.PlayRespawn(_config.RespawnGrowSeconds);
        }

        private void OnLevelledUp(HeroUnit unit)
        {
            if (!_displays.TryGetValue(unit, out var display)) return;
            display.PlayLevelUp(_config.LevelUpPopScale, _config.LevelUpPopSeconds);
            if (_config.LevelUpLabelSeconds > 0f) display.ShowLevel(unit.Level, _config.LevelUpLabelSeconds);
        }
    }
}
