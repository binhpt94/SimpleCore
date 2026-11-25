using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Castle;
using Game.Core;
using Game.Run;
using Game.Wallet;
using Game.Wave;

#pragma warning disable 67 // events a test does not raise

namespace Game.Hud.Tests
{
    internal sealed class FakeCastle : ICastle
    {
        public float Health { get; private set; } = 1000f;
        public float MaxHealth { get; private set; } = 1000f;
        public bool IsDestroyed => Health <= 0f;

        /// <summary>The castle falls: health 0, Destroyed raised (the run then moves on to Ending).</summary>
        public void Destroy()
        {
            Set(0f, MaxHealth);
            Destroyed?.Invoke();
        }
        public event Action HealthChanged;
        public event Action Destroyed;

        public void Set(float health, float max)
        {
            Health = health;
            MaxHealth = max;
            HealthChanged?.Invoke();
        }

        public int Listeners => HealthChanged?.GetInvocationList().Length ?? 0;
    }

    internal sealed class FakeSkills : ICastleSkills
    {
        private readonly Dictionary<CastleSkill, CastleSkillState> _states = new Dictionary<CastleSkill, CastleSkillState>();
        public readonly List<CastleSkill> Used = new List<CastleSkill>();

        public FakeSkills()
        {
            foreach (var skill in Skills) _states[skill] = new CastleSkillState(skill, true, 0f, Cooldown(skill));
        }

        public IReadOnlyList<CastleSkill> Skills { get; set; } = new[] { CastleSkill.Thunder, CastleSkill.Frost, CastleSkill.Rally };

        public CastleSkillState GetState(CastleSkill skill) => _states[skill];

        /// <summary>When true the castle turns every use down (for example the run has just ended) and nothing changes.</summary>
        public bool Refuse;

        public bool TryUse(CastleSkill skill)
        {
            Used.Add(skill);
            if (Refuse || !_states[skill].Ready) return false;
            _states[skill] = new CastleSkillState(skill, false, Cooldown(skill), Cooldown(skill));
            return true;
        }

        public void SetCooling(CastleSkill skill, float left, float total) => _states[skill] = new CastleSkillState(skill, false, left, total);

        public void SetReady(CastleSkill skill) => _states[skill] = new CastleSkillState(skill, true, 0f, Cooldown(skill));

        public void SetRaw(CastleSkill skill, bool ready, float left, float total) => _states[skill] = new CastleSkillState(skill, ready, left, total);

        private static float Cooldown(CastleSkill skill) => skill == CastleSkill.Thunder ? 20f : skill == CastleSkill.Frost ? 35f : 25f;
    }

    /// <summary>Castle's dev cheat: writes straight into the fake castle and skills, as the real one would.</summary>
    internal sealed class FakeCastleCheat : ICastleCheat
    {
        private readonly FakeCastle _castle;
        private readonly FakeSkills _skills;
        public List<string> Calls = new List<string>();

        public FakeCastleCheat(FakeCastle castle, FakeSkills skills)
        {
            _castle = castle;
            _skills = skills;
        }

        public void SetHealth(float health)
        {
            Calls.Add("health");
            _castle.Set(health, _castle.MaxHealth);
        }

        public void SetCooldownLeft(CastleSkill skill, float secondsLeft)
        {
            Calls.Add("cooldown " + skill);
            var total = _skills.GetState(skill).Cooldown;
            if (secondsLeft <= 0f) _skills.SetReady(skill);
            else _skills.SetCooling(skill, secondsLeft, total);
        }
    }

    internal sealed class FakeWallet : IWalletBalance, IWalletIncome, IWalletService
    {
        public int Coins { get; private set; } = 50;
        public event Action<int> CoinsChanged;

        public void Set(int coins)
        {
            Coins = coins;
            CoinsChanged?.Invoke(coins);
        }

        public void Add(int amount)
        {
            if (amount > 0) Set(Coins + amount);
        }

        public SpendResult TrySpend(int amount)
        {
            if (amount < 0) return SpendResult.InvalidAmount;
            if (amount > Coins) return SpendResult.NotEnoughCoins;
            Set(Coins - amount);
            return SpendResult.Ok;
        }
    }

    internal sealed class FakeWaves : IWaveProgress, IWaveCheat
    {
        public List<string> Calls = new List<string>();

        public void StartWave(int wave)
        {
            Calls.Add("wave " + wave);
            SetCurrent(wave);
            Start(wave);
        }

        public int CurrentWave { get; private set; } = 1;
        public event Action<int> CurrentWaveChanged;
        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;

        public void SetCurrent(int wave)
        {
            CurrentWave = wave;
            CurrentWaveChanged?.Invoke(wave);
        }

        public void Start(int wave) => WaveStarted?.Invoke(wave);
    }

    internal sealed class FakeRun : IRunFlow
    {
        public RunStep Step { get; private set; } = RunStep.Playing;
        public event Action<RunStep> StepChanged;
        public RunTeam Team => default;
        public event Action<RunTeam> RunStarted;
        public event Action BattleStarted;
        public event Action BattleStopped;
        public event Action BattleCleared;
        public bool IsPaused => Step == RunStep.Paused;

        public void SetStep(RunStep step)
        {
            Step = step;
            StepChanged?.Invoke(step);
        }

        public void StopBattle() => BattleStopped?.Invoke();

        /// <summary>How many times the HUD reported "pause pressed".</summary>
        public int PauseRequests;

        public void Play() { }
        public void EnterBattle(RunTeam team) { }

        /// <summary>Like the real run: counts the request and pauses only a running battle.</summary>
        public void Pause()
        {
            PauseRequests++;
            if (Step == RunStep.Playing) SetStep(RunStep.Paused);
        }

        public void Resume() { }
        public void Abandon() { }
        public void Replay() { }
        public void GoHome() { }
        public void Lose() { }
        public void CountClearedWave(int wave) { }
    }

    internal sealed class FakeView : IHudView
    {
        public bool Visible = true;
        public string Health;
        public float HealthFill;
        public string Coins;
        public string Wave;
        public string Banner;
        public bool BannerShown;
        public readonly List<string> SkillNames = new List<string>();
        public readonly List<int> SkillSlots = new List<int>();
        public readonly List<SkillShown> Skills = new List<SkillShown>();
        public bool Destroyed;
        public string CheatLabel;

        public event Action<int> SkillPressed;
        public event Action PausePressed;
        public event Action CheatPressed;
        public int SkillButtonsCreated;
        public HudInsets Insets;

        private UnityEngine.GameObject _root;

        /// <summary>A hidden object standing in for the view on screen; removed again by <see cref="Destroy"/>.</summary>
        public UnityEngine.Object Root =>
            _root != null ? _root : _root = new UnityEngine.GameObject("FakeHudView") { hideFlags = UnityEngine.HideFlags.HideAndDontSave };

        public void ApplySafeArea(HudInsets insets) => Insets = insets;
        public bool PauseShown = true;
        public void ShowPauseButton(bool shown) => PauseShown = shown;
        public void PressPause() => PausePressed?.Invoke();
        public int PauseListeners => PausePressed?.GetInvocationList().Length ?? 0;

        public void SetVisible(bool visible) => Visible = visible;

        public void ShowHealth(string text, float fill)
        {
            Health = text;
            HealthFill = fill;
        }

        public void ShowCoins(string text) => Coins = text;
        public void ShowWave(string text) => Wave = text;

        public void ShowBanner(string text)
        {
            Banner = text;
            BannerShown = true;
        }

        public void HideBanner() => BannerShown = false;

        public void CreateSkillButtons(IReadOnlyList<string> names, IReadOnlyList<int> slots)
        {
            SkillButtonsCreated++;
            SkillNames.Clear();
            SkillNames.AddRange(names);
            SkillSlots.Clear();
            SkillSlots.AddRange(slots);
            Skills.Clear();
            foreach (var _ in names) Skills.Add(default);
        }

        public void ShowSkill(int index, bool ready, string seconds, float cover, float scale) =>
            Skills[index] = new SkillShown { Ready = ready, Seconds = seconds, Cover = cover, Scale = scale };

        public void ShowCheatButton(string label) => CheatLabel = label;

        public void Destroy()
        {
            Destroyed = true;
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        public void Press(int index) => SkillPressed?.Invoke(index);
        public void PressCheat() => CheatPressed?.Invoke();

        public int SkillListeners => SkillPressed?.GetInvocationList().Length ?? 0;

        public struct SkillShown
        {
            public bool Ready;
            public string Seconds;
            public float Cover;
            public float Scale;
        }
    }

    internal sealed class FakeSpawner : IHudViewSpawner
    {
        public readonly FakeView View = new FakeView();
        public int Spawned;

        public IHudView Spawn()
        {
            Spawned++;
            return View;
        }
    }

    internal sealed class FakeClock : IHudClock
    {
        public event Action<float> Ticked;
        public bool Running;

        public void Start() => Running = true;
        public void Stop() => Running = false;

        public void Tick(float seconds)
        {
            if (Running) Ticked?.Invoke(seconds);
        }
    }

    internal sealed class FakeSessionScene : ISessionScene
    {
        public readonly List<ISessionParticipant> Joined = new List<ISessionParticipant>();
        public SessionState State => SessionState.Init;
        public event Action<SessionState> StateChanged;
        public void Join(ISessionParticipant participant) => Joined.Add(participant);
        public UniTask EndAsync() => UniTask.CompletedTask;
    }

    internal sealed class FakeRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<T>(T service) where T : class => _services[typeof(T)] = service;

        public T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service)) return (T)service;
            throw new InvalidOperationException("Missing service " + typeof(T).Name);
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            service = _services.TryGetValue(typeof(T), out var found) ? (T)found : null;
            return service != null;
        }
    }

    internal static class HudTestTimings
    {
        public static HudTimings Default => new HudTimings(1.5f, 1.2f, 0.3f);
    }
}
