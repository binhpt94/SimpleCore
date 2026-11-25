using System;
using System.Collections.Generic;
using Game.Hero;

namespace Game.TeamSelect.Tests
{
    /// <summary>Fakes of Hero and of the screen flow for the team select tests, plus a recording view.</summary>
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

    internal sealed class FakeView : ITeamSelectView
    {
        public bool Visible;
        public string BuildError;
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
        public void ShowBuildError(string text) => BuildError = text;
    }

    /// <summary>
    /// A screen wired to fakes, with a fake screen flow (GameFlow): <see cref="Open"/> shows the screen like pressing
    /// Play. On Enter battle the fake flow records the team and, while <see cref="BuildSucceeds"/>, remembers it and
    /// hides the screen (battle built); otherwise it waits, like a battle still being built. On Back it records the
    /// tap and hides the screen (back to the home screen).
    /// </summary>
    internal sealed class TestScreen
    {
        public readonly List<string[]> Entered = new List<string[]>();
        public int BackCount;
        public bool BuildSucceeds = true;
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
            Presenter = new TeamSelectPresenter(Roster, Rules, Memory);
            Presenter.Attach(View, Portraits);
            Presenter.EnterBattlePressed += team =>
            {
                var codes = new string[team.Count];
                for (var i = 0; i < codes.Length; i++) codes[i] = team[i];
                Entered.Add(codes);
                if (!BuildSucceeds) return;
                Presenter.RememberTeam(team);
                Presenter.Hide();
            };
            Presenter.BackPressed += () =>
            {
                BackCount++;
                Presenter.Hide();
            };
        }

        public TestScreen Open()
        {
            Presenter.Show();
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
