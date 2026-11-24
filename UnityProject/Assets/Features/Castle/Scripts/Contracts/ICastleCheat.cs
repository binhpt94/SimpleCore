#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Game.Castle
{
    /// <summary>
    /// Dev-only way to force the castle into a cheat state without playing up to it. Not in release builds.
    /// Every change is reported through the usual <see cref="ICastle.HealthChanged"/> and skill states.
    /// </summary>
    public interface ICastleCheat
    {
        /// <summary>
        /// Sets the castle's health, clamped to 1..MaxHealth: a cheat never destroys the castle.
        /// Does nothing once the castle is destroyed.
        /// </summary>
        void SetHealth(float health);

        /// <summary>
        /// Puts the skill on cooldown with <paramref name="secondsLeft"/> left, clamped to 0..its full cooldown;
        /// 0 makes it ready. Does not use the skill (no effect happens).
        /// </summary>
        void SetCooldownLeft(CastleSkill skill, float secondsLeft);
    }
}
#endif
