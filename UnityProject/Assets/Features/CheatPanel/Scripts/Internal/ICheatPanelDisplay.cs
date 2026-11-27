using System.Collections.Generic;

namespace Game.CheatPanel
{
    /// <summary>What the cheat overlay shows; the view draws it as is.</summary>
    internal readonly struct CheatPanelScreen
    {
        public CheatPanelScreen(string buttonLabel, bool isOpen, string title, IReadOnlyList<CheatRow> rows,
            int activeRow, bool rowsLocked, string emptyLabel)
        {
            ButtonLabel = buttonLabel;
            IsOpen = isOpen;
            Title = title;
            Rows = rows;
            ActiveRow = activeRow;
            RowsLocked = rowsLocked;
            EmptyLabel = emptyLabel;
        }

        public string ButtonLabel { get; }

        /// <summary>Panel and shade shown; the button shows its "open" look.</summary>
        public bool IsOpen { get; }

        public string Title { get; }
        public IReadOnlyList<CheatRow> Rows { get; }

        /// <summary>Index in <see cref="Rows"/> of the row marked as chosen.</summary>
        public int ActiveRow { get; }

        /// <summary>A scene switch is running: every row is locked, the button still opens and closes the panel.</summary>
        public bool RowsLocked { get; }

        /// <summary>Shown under the normal row when there is no state row; null otherwise.</summary>
        public string EmptyLabel { get; }
    }

    /// <summary>What the player does on the overlay.</summary>
    internal interface ICheatPanelInput
    {
        void PressButton();
        void PressRow(int index);
    }

    /// <summary>The cheat overlay view (button, shade, panel). Lives on the overlay object.</summary>
    internal interface ICheatPanelDisplay
    {
        void Bind(ICheatPanelInput input);
        void Show(CheatPanelScreen screen);
    }
}
