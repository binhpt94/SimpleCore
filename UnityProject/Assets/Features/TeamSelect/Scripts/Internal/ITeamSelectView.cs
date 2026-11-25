using System.Collections.Generic;

namespace Game.TeamSelect
{
    /// <summary>What one team slot shows: the hero in it (code and name), or empty.</summary>
    internal readonly struct SlotState
    {
        public SlotState(string heroCode, string heroName)
        {
            HeroCode = heroCode;
            HeroName = heroName;
        }

        public bool IsEmpty => HeroCode == null;
        public string HeroCode { get; }
        public string HeroName { get; }
    }

    /// <summary>What the player can do on the team select screen; the view forwards each tap here.</summary>
    internal interface ITeamSelectInput
    {
        void TapCard(string heroCode);

        void TapSlot(int index);

        bool TapStart();

        /// <summary>The device back key.</summary>
        void TapBack();
    }

    /// <summary>
    /// The team select screen as the presenter drives it (design: team-slots/slots-spec#screen-parts):
    /// title, the hero cards, the team slots, one Enter battle button. The view only shows and forwards taps.
    /// </summary>
    internal interface ITeamSelectView
    {
        /// <summary>Where the view sends the player's taps.</summary>
        void Bind(ITeamSelectInput input);

        void SetVisible(bool visible);

        void ShowCards(IReadOnlyList<HeroCard> cards);

        void ShowSlots(IReadOnlyList<SlotState> slots);

        /// <summary>Enter battle button: true = can be pressed, false = locked (looks different).</summary>
        void ShowStartButton(bool enabled);
    }

    /// <summary>Creates and destroys the screen's view (a prefab in the game, a fake in tests).</summary>
    internal interface ITeamSelectViewFactory
    {
        ITeamSelectView Create();

        void Destroy(ITeamSelectView view);
    }
}
