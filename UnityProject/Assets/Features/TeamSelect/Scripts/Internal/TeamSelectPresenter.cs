using System;
using System.Collections.Generic;
using Game.Hero;
using Game.Run;

namespace Game.TeamSelect
{
    /// <summary>
    /// The team select screen (design: feature-team-select). Opens when the run reaches
    /// <see cref="RunStep.TeamSelect"/>: fills the slots from the remembered team (or the default team), applies
    /// the tap rules, and on Enter battle with a full team remembers the team and hands it to the run once.
    /// After that the screen takes no more taps until it opens again.
    /// </summary>
    internal sealed class TeamSelectPresenter : ITeamSelectInput, IDisposable
    {
        private readonly IRunFlow _run;
        private readonly IHeroRoster _roster;
        private readonly TeamSelectRules _rules;
        private readonly TeamMemory _memory;
        private readonly IHeroPortraits _portraits;
        private readonly TeamSlots _slots;
        private ITeamSelectView _view;

        public TeamSelectPresenter(IRunFlow run, IHeroRoster roster, TeamSelectRules rules, TeamMemory memory, IHeroPortraits portraits)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _portraits = portraits;
            _slots = new TeamSlots(rules.SlotCount);
            _slots.Changed += Refresh;
            _run.StepChanged += OnStepChanged;
        }

        public TeamSlots Slots => _slots;

        /// <summary>True while the screen is up and takes taps.</summary>
        public bool IsOpen { get; private set; }

        public bool CanStart => IsOpen && _slots.IsFull;

        public IReadOnlyList<HeroCard> Cards => HeroCards.Build(_roster, _slots, _rules, _portraits);

        public void Attach(ITeamSelectView view)
        {
            _view = view;
            _view?.Bind(this);
            if (_run.Step == RunStep.TeamSelect) Open();
            else _view?.SetVisible(false);
        }

        public void Dispose()
        {
            _run.StepChanged -= OnStepChanged;
            _slots.Changed -= Refresh;
            _view = null;
        }

        private void OnStepChanged(RunStep step)
        {
            if (step == RunStep.TeamSelect) Open();
            else Close();
        }

        /// <summary>Opens the screen with the remembered team (or the default team) in the slots.</summary>
        public void Open()
        {
            IsOpen = true;
            _slots.Fill(_memory.TeamToShow(_rules.DefaultTeam), code => _roster.TryGet(code, out _));
            _view?.SetVisible(true);
            Refresh();
        }

        private void Close()
        {
            IsOpen = false;
            _view?.SetVisible(false);
        }

        public void TapCard(string heroCode)
        {
            if (!IsOpen) return;
            _slots.TapHero(heroCode);
        }

        public void TapSlot(int index)
        {
            if (!IsOpen) return;
            _slots.TapSlot(index);
        }

        /// <summary>The device back key: the screen has no way back, so nothing happens.</summary>
        public void TapBack()
        {
        }

        /// <summary>Enter battle: only with a full team, only once per opening.</summary>
        public bool TapStart()
        {
            if (!CanStart) return false;
            var team = _slots.ToArray();
            IsOpen = false;
            _memory.Remember(team);
            Refresh();
            _run.EnterBattle(new RunTeam(team));
            return true;
        }

        private void Refresh()
        {
            if (_view == null) return;
            _view.ShowCards(Cards);
            var slots = new SlotState[_slots.Count];
            for (var i = 0; i < slots.Length; i++)
            {
                var code = _slots[i];
                string name = null;
                if (code != null && _roster.TryGet(code, out var hero)) name = hero.Name;
                slots[i] = new SlotState(code, name);
            }
            _view.ShowSlots(slots);
            _view.ShowStartButton(CanStart);
        }
    }
}
