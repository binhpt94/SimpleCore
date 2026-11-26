using System;

namespace Game.Enemy
{
    /// <summary>
    /// Where enemies are drawn. Implemented by the view; the presenter pushes what to draw after each step of the
    /// shared clock. The view only animates between those states (bob, lunge, flash, shrink); it moves no enemy.
    /// Positions are battlefield units (u), ground at y = 0.
    /// </summary>
    internal interface IEnemyStage
    {
        /// <summary>
        /// Reports to the session every object the stage holds: the stage itself now, then each enemy, shot and coin
        /// text as it is created. The stage disposes each handle when it destroys that object.
        /// </summary>
        void TrackWith(Func<UnityEngine.Object, IDisposable> trackView);

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

        /// <summary>The enemy came on screen with an entrance: grow it and shrink it back to its size.</summary>
        void Enter(int handle);

        /// <summary>The enemy was defeated: shrink it away, then stop drawing it.</summary>
        void Defeat(int handle);

        /// <summary>Stops drawing every enemy at once.</summary>
        void RemoveAll();

        /// <summary>Freezes (or lets run again) every running animation, flying shots and rising coin texts included.</summary>
        void SetFrozen(bool frozen);

        /// <summary>Destroys the stage and everything on it.</summary>
        void Close();
    }
}
