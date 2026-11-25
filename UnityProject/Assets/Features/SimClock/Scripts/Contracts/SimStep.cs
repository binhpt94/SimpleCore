namespace Game.SimClock
{
    /// <summary>One step of the shared clock, as handed to each subscriber.</summary>
    public readonly struct SimStep
    {
        public SimStep(long number, long gameplayNumber, float seconds, bool gameplay)
        {
            Number = number;
            GameplayNumber = gameplayNumber;
            Seconds = seconds;
            Gameplay = gameplay;
        }

        /// <summary>Number of this step among all steps of the run, from 1.</summary>
        public long Number { get; }

        /// <summary>Gameplay steps counted so far including this one; unchanged on a step that is not gameplay.</summary>
        public long GameplayNumber { get; }

        /// <summary>Length of this step in seconds.</summary>
        public float Seconds { get; }

        /// <summary>True when the run lets the battle play on this step; gameplay waits and movement only advance then.</summary>
        public bool Gameplay { get; }
    }
}
