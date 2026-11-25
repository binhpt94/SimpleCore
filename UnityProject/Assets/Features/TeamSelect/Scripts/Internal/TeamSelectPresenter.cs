using System;
using System.Collections.Generic;
using Game.Hero;

namespace Game.TeamSelect
{
    /// <summary>
    /// The team select screen (design: feature-team-select/team-slots/slots-spec; routing:
    /// feature-game-flow/flow-routes/routes-values).
    /// The screen flow shows and hides it. On show the slots are filled from the remembered team (or the default
    /// team); taps follow the slot rules. Enter battle with a full team reports the team once and locks the screen
    /// until the flow unlocks it (a failed build) or hides it. The remembered team changes only when the flow says
    /// a battle was built for a team (<see cref="RememberTeam"/>). Back (button or device key) is reported.
    /// </summary>
    internal sealed class TeamSelectPresenter : ITeamSelectInput, IDisposable
    {
        private readonly IHeroRoster _roster;
        private readonly TeamSelectRules _rules;
        private readonly TeamMemory _memory;
        private readonly TeamSlots _slots;
        private ITeamSelectView _view;
        private IHeroPortraits _portraits;
        private bool _locked;
        private string _buildError;

        public TeamSelectPresenter(IHeroRoster roster, TeamSelectRules rules, TeamMemory memory)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _slots = new TeamSlots(rules.SlotCount);
            _slots.Changed += Refresh;
        }

        /// <summary>Enter battle with a full team: hero codes in slot order.</summary>
        public event Action<IReadOnlyList<string>> EnterBattlePressed;

        /// <summary>The Back button or the device back key, while the screen is up and not waiting on a battle.</summary>
        public event Action BackPressed;

        public TeamSlots Slots => _slots;

        /// <summary>True while the screen is shown.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>True from Enter battle (or a lock from the flow) until the flow unlocks or hides the screen.</summary>
        public bool IsLocked => _locked;

        public bool TakesTaps => IsOpen && !_locked;

        public bool CanStart => TakesTaps && _slots.IsFull;

        /// <summary>The build error line now shown, or null.</summary>
        public string BuildError => _buildError;

        public IReadOnlyList<HeroCard> Cards => HeroCards.Build(_roster, _slots, _rules, _portraits);

        /// <summary>Hooks the view (and the portraits it has); the view shows the current state at once.</summary>
        public void Attach(ITeamSelectView view, IHeroPortraits portraits)
        {
            _view = view;
            _portraits = portraits;
            if (_view == null) return;
            _view.Bind(this);
            _view.SetVisible(IsOpen);
            _view.ShowBuildError(_buildError);
            Refresh();
        }

        public void Detach()
        {
            _view = null;
            _portraits = null;
        }

        public void Dispose()
        {
            _slots.Changed -= Refresh;
            EnterBattlePressed = null;
            BackPressed = null;
            Detach();
        }

        /// <summary>Shows the screen with the remembered team (or the default team) in the slots.</summary>
        public void Show()
        {
            IsOpen = true;
            _locked = false;
            _buildError = null;
            _slots.Fill(_memory.TeamToShow(_rules.DefaultTeam), code => _roster.TryGet(code, out _));
            _view?.SetVisible(true);
            _view?.ShowBuildError(null);
            Refresh();
        }

        /// <summary>Hides the screen; slots picked but not confirmed are dropped (the next show refills them).</summary>
        public void Hide()
        {
            IsOpen = false;
            _locked = false;
            _buildError = null;
            _view?.ShowBuildError(null);
            _view?.SetVisible(false);
        }

        /// <summary>The flow locks the screen while a battle is built, and unlocks it when the build failed.</summary>
        public void SetEnterBattleEnabled(bool enabled)
        {
            _locked = !enabled;
            Refresh();
        }

        public void ShowBuildError(string text)
        {
            _buildError = string.IsNullOrEmpty(text) ? null : text;
            _view?.ShowBuildError(_buildError);
        }

        public void HideBuildError() => ShowBuildError(null);

        /// <summary>A battle was built for <paramref name="heroCodes"/>: it becomes the remembered team.</summary>
        public void RememberTeam(IReadOnlyList<string> heroCodes) => _memory.Remember(heroCodes);

        public void TapCard(string heroCode)
        {
            if (!TakesTaps) return;
            _slots.TapHero(heroCode);
        }

        public void TapSlot(int index)
        {
            if (!TakesTaps) return;
            _slots.TapSlot(index);
        }

        /// <summary>The Back button and the device back key: reported while the screen takes taps.</summary>
        public void TapBack()
        {
            if (!TakesTaps) return;
            BackPressed?.Invoke();
        }

        /// <summary>
        /// Enter battle (slots-spec#start-button-once): only with a full team; reports the team once and locks the
        /// screen until the flow has the build result (unlocks on failure, hides on success).
        /// </summary>
        public bool TapStart()
        {
            if (!CanStart) return false;
            var team = _slots.ToArray();
            _locked = true;
            HideBuildError(); // retrying after a failed build: the error line goes away on the tap
            Refresh();
            EnterBattlePressed?.Invoke(team);
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
