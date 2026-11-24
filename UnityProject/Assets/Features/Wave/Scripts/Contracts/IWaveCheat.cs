#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Game.Wave
{
    /// <summary>Dev-only way to jump to a wave without playing up to it. Not in release builds.</summary>
    public interface IWaveCheat
    {
        /// <summary>
        /// While the battle runs: drops what is left to send of the current wave (or the break) and starts
        /// <paramref name="wave"/> (from 1) at once, exactly as a normal start: CurrentWaveChanged, then WaveStarted,
        /// first enemy at once. Enemies already on the field stay. No reward and no score for the skipped waves.
        /// Does nothing outside a running battle.
        /// </summary>
        void StartWave(int wave);
    }
}
#endif
