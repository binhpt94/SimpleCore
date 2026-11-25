namespace Game.PopupLose
{
    /// <summary>The text lines of the lose popup, ready to show (result-spec).</summary>
    internal readonly struct LoseResultText
    {
        internal const string TitleText = "Thành đã bị phá";
        internal const string NewRecordText = "Kỉ lục mới!";
        internal const string AdFailedText = "Quảng cáo chưa hoàn tất";

        private LoseResultText(string title, string score, string best, bool showNewRecord, bool showAdFailed)
        {
            Title = title;
            Score = score;
            Best = best;
            ShowNewRecord = showNewRecord;
            ShowAdFailed = showAdFailed;
        }

        internal string Title { get; }

        /// <summary>"Trụ được N đợt".</summary>
        internal string Score { get; }

        /// <summary>"Kỉ lục: M đợt".</summary>
        internal string Best { get; }

        /// <summary>True only when the run just set a new best score: the "Kỉ lục mới!" line is shown.</summary>
        internal bool ShowNewRecord { get; }

        /// <summary>
        /// True when the run was lost because the revive ad was not watched to the end or did not load, and no new
        /// best was set: plain "Quảng cáo chưa hoàn tất" in the place of the "Kỉ lục mới!" line. A new best wins.
        /// </summary>
        internal bool ShowAdFailed { get; }

        internal static LoseResultText From(int score, int best, bool isNewBest, bool lostAfterFailedRevive) =>
            new LoseResultText(TitleText, $"Trụ được {score} đợt", $"Kỉ lục: {best} đợt",
                isNewBest, lostAfterFailedRevive && !isNewBest);
    }
}
