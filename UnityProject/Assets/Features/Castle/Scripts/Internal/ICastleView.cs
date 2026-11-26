using System;

namespace Game.Castle
{
    /// <summary>
    /// What the castle's view shows. Positions in battlefield units (u), x = 0 at the castle centre, ground at y = 0.
    /// The view only draws; it times its own effects with the seconds given to <see cref="Advance"/>.
    /// </summary>
    internal interface ICastleView
    {
        /// <summary>Every frame, with the frame's seconds.</summary>
        event Action<float> Ticked;

        /// <summary>The Unity object the session counts as this view while it is held.</summary>
        UnityEngine.Object ViewObject { get; }

        /// <summary>Shakes the castle lightly for <paramref name="seconds"/> (PROTOTYPE hit feedback); a new shake starts over.</summary>
        void ShakeHit(float seconds);

        /// <summary>Shows the damaged picture of the castle (health under half) or the whole one.</summary>
        void ShowDamaged(bool damaged);

        void Place(float halfWidth, float height);

        void SetVisible(bool visible);

        /// <summary>Back to full size, no flash.</summary>
        void ResetBody();

        /// <summary>Flashes the castle for <paramref name="seconds"/>; a new flash starts over.</summary>
        void FlashHit(float seconds);

        /// <summary>Grows the castle to <paramref name="scale"/> and back over <paramref name="seconds"/>; a new pop starts over.</summary>
        void PopLevelUp(float scale, float seconds);

        /// <summary>Shrinks the castle to nothing over <paramref name="seconds"/>.</summary>
        void Collapse(float seconds);

        /// <summary>A shot drawing flies from <paramref name="fromX"/> to <paramref name="toX"/> in <paramref name="seconds"/>.</summary>
        void ShowShot(int targetHandle, float fromX, float toX, float seconds);

        /// <summary>The enemy <paramref name="targetHandle"/> is gone: shot drawings flying to it vanish.</summary>
        void DropShotsAt(int targetHandle);

        /// <summary>The Thunder effect on an enemy at <paramref name="x"/>, shown for <paramref name="seconds"/>.</summary>
        void ShowThunder(float x, float seconds);

        /// <summary>Moves every running effect on by <paramref name="seconds"/>.</summary>
        void Advance(float seconds);

        void ClearEffects();
    }

    /// <summary>Makes and removes the castle's view (Unity objects), so the presenter runs in tests.</summary>
    internal interface ICastleViewSpawner
    {
        ICastleView Spawn();

        void Despawn(ICastleView view);
    }
}
