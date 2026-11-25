namespace Game.Run
{
    /// <summary>Where the run reports programming errors of other features. Tests use a fake.</summary>
    internal interface IRunLog
    {
        void Error(string message);
    }
}
