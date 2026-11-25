using System;
using System.Collections.Generic;

namespace Game.TeamSelect
{
    /// <summary>
    /// The team select screen (feature-team-select), shown and hidden by the screen flow. It reports Enter battle and
    /// Back; the flow drives the Enter battle button and the build error line while a battle is being built.
    /// Teams are hero codes in slot order (slot 1 first).
    /// </summary>
    public interface ITeamSelectScreen
    {
        /// <summary>Shows the screen with the slots filled from the remembered team (or the default team).</summary>
        void Show();

        /// <summary>Hides the screen; slots picked but not confirmed are dropped.</summary>
        void Hide();

        /// <summary>Locks or unlocks the Enter battle button (locked while a battle is being built).</summary>
        void SetEnterBattleEnabled(bool enabled);

        /// <summary>Shows <paramref name="text"/> on the build error line.</summary>
        void ShowBuildError(string text);

        void HideBuildError();

        /// <summary>A battle was built for <paramref name="heroCodes"/>: it becomes the remembered team.</summary>
        void RememberTeam(IReadOnlyList<string> heroCodes);

        /// <summary>The player tapped Enter battle with a full team, given as hero codes in slot order.</summary>
        event Action<IReadOnlyList<string>> EnterBattlePressed;

        /// <summary>The player tapped Back (or the device back key) on this screen.</summary>
        event Action BackPressed;
    }
}
