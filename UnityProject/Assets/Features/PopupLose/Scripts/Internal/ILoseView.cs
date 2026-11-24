using System;

namespace Game.PopupLose
{
    /// <summary>What the lose popup prefab shows and reports. Implemented in View, on the popup root.</summary>
    internal interface ILoseView
    {
        /// <summary>The player touched "Chơi lại".</summary>
        event Action ReplayPressed;

        void ShowResult(LoseResultText text);

        /// <summary>After the first touch: every button stops taking touches.</summary>
        void LockButtons();
    }
}
