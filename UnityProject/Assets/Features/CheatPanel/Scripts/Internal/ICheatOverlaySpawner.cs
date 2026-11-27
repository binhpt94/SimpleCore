using UnityEngine;

namespace Game.CheatPanel
{
    /// <summary>Creates and destroys the cheat overlay (button, shade, panel) on top of everything in the current scene.</summary>
    internal interface ICheatOverlaySpawner
    {
        GameObject Spawn(CheatPanelConfig config);
        void Despawn(GameObject overlay);
    }
}
