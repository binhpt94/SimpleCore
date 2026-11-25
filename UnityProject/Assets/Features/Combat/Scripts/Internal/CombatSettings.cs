namespace Game.Combat
{
    /// <summary>Balance numbers of combat, read from <see cref="CombatConfig"/>.</summary>
    internal readonly struct CombatSettings
    {
        public CombatSettings(float stepSeconds, int maxStepsPerFrame)
        {
            StepSeconds = stepSeconds;
            MaxStepsPerFrame = maxStepsPerFrame;
        }

        /// <summary>Length of one fixed step, and of one combat tick.</summary>
        public float StepSeconds { get; }

        public int MaxStepsPerFrame { get; }
    }
}
