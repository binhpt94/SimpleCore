using System;
using System.Collections.Generic;
using Game.Hero;
using Game.Run;

namespace Game.TeamSelect.Tests
{
    /// <summary>Fakes of Hero and Run for the team select tests, plus a recording view.</summary>
    internal static class Fakes
    {
        public const string Knight = "knight";
        public const string Axeman = "axeman";
        public const string Swordsman = "swordsman";
        public const string Archer = "archer";
        public const string Mage = "mage";

        public static readonly string[] DefaultTeam = { Knight, Axeman, Archer };

        public static List<HeroInfo> FiveHeroes() => new List<HeroInfo>
        {
            new HeroInfo(Knight, "Hiệp sĩ", HeroGroup.Melee, HeroAttack.SingleTarget),
            new HeroInfo(Axeman, "Đấu sĩ rìu", HeroGroup.Melee, HeroAttack.FrontCleave),
            new HeroInfo(Swordsman, "Kiếm khách", HeroGroup.Melee, HeroAttack.SingleTarget),
            new HeroInfo(Archer, "Cung thủ", HeroGroup.Ranged, HeroAttack.SingleTarget),
            new HeroInfo(Mage, "Pháp sư", HeroGroup.Ranged, HeroAttack.Blast),
        };

        public static TeamSelectRules Rules() => TeamSelectRules.Design(DefaultTeam, new Dictionary<string, string>
        {
            { Knight, "Chắn" },
            { Swordsman, "Đánh nhanh" },
            { Archer, "Tầm xa" },
        });
    }

    internal sealed class FakeRoster : IHeroRoster
    {
        public readonly List<HeroInfo> List;

        public FakeRoster() : this(Fakes.FiveHeroes()) { }

        public FakeRoster(List<HeroInfo> heroes) { List = heroes; }

        public IReadOnlyList<HeroInfo> Heroes => List;

        public bool TryGet(string code, out HeroInfo hero)
        {
            foreach (var h in List)
                if (h.Code == code) { hero = h; return true; }
            hero = default;
            return false;
        }
    }

    internal sealed class FakePortraits : IHeroPortraits
    {
        public readonly HashSet<string> Missing = new HashSet<string>();
        public bool Has(string heroCode) => !Missing.Contains(heroCode);
    }

    internal sealed class FakeRun : IRunFlow
    {
        public readonly List<RunTeam> Entered = new List<RunTeam>();
        public bool MoveToPlayingOnEnter = true;

        public RunStep Step { get; private set; } = RunStep.Home;
        public event Action<RunStep> StepChanged;
        public RunTeam Team { get; private set; }
        public event Action<RunTeam> RunStarted { add { } remove { } }
        public event Action BattleStarted { add { } remove { } }
        public event Action BattleStopped { add { } remove { } }
        public event Action BattleCleared { add { } remove { } }
        public bool IsPaused => Step == RunStep.Paused;

        public void SetStep(RunStep step)
        {
            Step = step;
            StepChanged?.Invoke(step);
        }

        public void Play() => SetStep(RunStep.TeamSelect);

        public void EnterBattle(RunTeam team)
        {
            Entered.Add(team);
            Team = team;
            if (MoveToPlayingOnEnter) SetStep(RunStep.Playing);
        }

        public void Pause() { }
        public void Resume() { }
        public void Abandon() => SetStep(RunStep.Home);
        public void Replay() => SetStep(RunStep.TeamSelect);
        public void GoHome() => SetStep(RunStep.Home);
        public void Lose() => SetStep(RunStep.Lost);
        public void CountClearedWave(int wave) { }
    }

    internal sealed class FakeView : ITeamSelectView
    {
        public bool Visible;
        public IReadOnlyList<HeroCard> Cards = Array.Empty<HeroCard>();
        public IReadOnlyList<SlotState> Slots = Array.Empty<SlotState>();
        public bool StartEnabled;
        public int CardsShown;
        public ITeamSelectInput Input;

        public void Bind(ITeamSelectInput input) => Input = input;

        public void SetVisible(bool visible) => Visible = visible;

        public void ShowCards(IReadOnlyList<HeroCard> cards)
        {
            Cards = cards;
            CardsShown++;
        }

        public void ShowSlots(IReadOnlyList<SlotState> slots) => Slots = slots;
        public void ShowStartButton(bool enabled) => StartEnabled = enabled;
    }

    /// <summary>A screen wired to fakes; <see cref="Open"/> goes Home → TeamSelect like pressing Play.</summary>
    internal sealed class TestScreen
    {
        public readonly FakeRun Run = new FakeRun();
        public readonly FakeRoster Roster;
        public readonly FakePortraits Portraits = new FakePortraits();
        public readonly FakeView View = new FakeView();
        public readonly TeamMemory Memory;
        public readonly TeamSelectRules Rules;
        public readonly TeamSelectPresenter Presenter;

        public TestScreen(TeamMemory memory = null, FakeRoster roster = null, TeamSelectRules rules = null)
        {
            Memory = memory ?? new TeamMemory();
            Roster = roster ?? new FakeRoster();
            Rules = rules ?? Fakes.Rules();
            Presenter = new TeamSelectPresenter(Run, Roster, Rules, Memory, Portraits);
            Presenter.Attach(View);
        }

        public TestScreen Open()
        {
            Run.Play();
            return this;
        }

        public string[] SlotCodes()
        {
            var codes = new string[Presenter.Slots.Count];
            for (var i = 0; i < codes.Length; i++) codes[i] = Presenter.Slots[i];
            return codes;
        }

        public void Clear()
        {
            for (var i = 0; i < Presenter.Slots.Count; i++) Presenter.TapSlot(i);
        }
    }
}
