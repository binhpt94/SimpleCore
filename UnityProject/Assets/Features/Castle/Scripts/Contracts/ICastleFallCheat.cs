#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Game.Castle
{
    /// <summary>
    /// Dev-only: destroys the castle on purpose, for cheat states that start right after a fall. Not in release builds.
    /// </summary>
    public interface ICastleFallCheat
    {
        /// <summary>
        /// Takes the castle to 0 health through the usual fall path (feature-castle/castle-core/castle-body-values):
        /// it is reported to the run exactly once, like a real fall, so the run's own rules decide what comes next
        /// (the run is lost). Does nothing when the castle is already destroyed or no battle is running.
        /// </summary>
        void Fall();
    }
}
#endif
