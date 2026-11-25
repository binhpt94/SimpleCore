using UnityEngine;

namespace Game.PopupPause
{
    /// <summary>The number of the current frame; tests replace it.</summary>
    internal interface IFrameClock
    {
        int Frame { get; }
    }

    internal sealed class UnityFrameClock : IFrameClock
    {
        public int Frame => Time.frameCount;
    }
}
