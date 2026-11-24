namespace Game.PopupLose
{
    /// <summary>The text lines of the lose popup, ready to show (result-spec).</summary>
    internal readonly struct LoseResultText
    {
        internal const string TitleText = "Thành đã bị phá";
        internal const string NewRecordText = "Kỉ lục mới!";

        private LoseResultText(string title, string score, string best, bool showNewRecord)
        {
            Title = title;
            Score = score;
            Best = best;
            ShowNewRecord = showNewRecord;
        }

        internal string Title { get; }

        /// <summary>"Trụ được N đợt".</summary>
        internal string Score { get; }

        /// <summary>"Kỉ lục: M đợt".</summary>
        internal string Best { get; }

        /// <summary>True only when the run just set a new best score: the "Kỉ lục mới!" line is shown.</summary>
        internal bool ShowNewRecord { get; }

        internal static LoseResultText From(int score, int best, bool isNewBest) =>
            new LoseResultText(TitleText, $"Trụ được {score} đợt", $"Kỉ lục: {best} đợt", isNewBest);
    }
}
