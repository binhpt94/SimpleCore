using System;

namespace Game.Enemy
{
    /// <summary>
    /// Where enemies are drawn, and the frame clock that moves them. Implemented by the view; the presenter pushes
    /// what to draw. Positions are battlefield units (u), ground at y = 0.
    /// </summary>
    internal interface IEnemyStage
    {
        /// <summary>Raised once per rendered frame with the seconds since the last one.</summary>
        event Action<float> Frame;

        /// <summary>Starts drawing an enemy.</summary>
        void Show(EnemyLook look);

        /// <summary>Puts an enemy where it is now; walking enemies bob.</summary>
        void Move(EnemyLook look);

        /// <summary>The enemy hit its foe: nudge toward <paramref name="direction"/>.</summary>
        void Lunge(int handle, int direction);

        /// <summary>A ranged enemy shot its foe: draw the shot flying from the enemy to the foe.</summary>
        void Shoot(EnemyShot shot);

        /// <summary>A defeated enemy paid coins: show "+coins" where it stood, rising, then gone.</summary>
        void ShowCoins(EnemyReward reward);

        /// <summary>The enemy was hit: flash it.</summary>
        void Flash(int handle);

        /// <summary>The enemy was defeated: shrink it away, then stop drawing it.</summary>
        void Defeat(int handle);

        /// <summary>Stops drawing every enemy at once.</summary>
        void RemoveAll();

        /// <summary>Freezes (or resumes) every running animation, flying shots and rising coin texts included.</summary>
        void SetPaused(bool paused);

        /// <summary>Destroys the stage and everything on it.</summary>
        void Close();
    }
}
