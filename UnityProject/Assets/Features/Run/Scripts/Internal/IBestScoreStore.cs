namespace Game.Run
{
    /// <summary>Where the best score lives on the device. Tests replace it with a fake.</summary>
    internal interface IBestScoreStore
    {
        /// <summary>The saved best score; a missing or unreadable value may come back as any number.</summary>
        int Load();

        /// <summary>Writes the best score. May throw when the device refuses.</summary>
        void Save(int best);
    }
}
