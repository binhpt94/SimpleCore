using System;

namespace Game.PopupLose
{
    /// <summary>What the lose popup prefab shows and reports. Implemented in View, on the popup root.</summary>
    internal interface ILoseView
    {
        /// <summary>The player touched "Chơi lại" (main button).</summary>
        event Action ReplayPressed;

        /// <summary>The player touched "Về màn bắt đầu" (second button).</summary>
        event Action HomePressed;

        void ShowResult(LoseResultText text);

        /// <summary>Shows or hides "Về màn bắt đầu". Hidden at IDEA (no start screen yet, node actions-spec).</summary>
        void ShowHomeButton(bool shown);

        /// <summary>After the first touch: both buttons stop taking touches.</summary>
        void LockButtons();
    }
}
