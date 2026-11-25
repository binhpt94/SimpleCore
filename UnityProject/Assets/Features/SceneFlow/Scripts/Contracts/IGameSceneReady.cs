using System;

namespace Game.SceneFlow
{
    /// <summary>
    /// Tells the Game scene's features when the scene is ready for the player: every participant has run and the
    /// loading cover is being lifted. Raised once per load of the Game scene.
    /// </summary>
    public interface IGameSceneReady
    {
        /// <summary>True once <see cref="Ready"/> was raised for this load of the scene.</summary>
        bool IsReady { get; }

        /// <summary>Raised once when the Game scene is ready.</summary>
        event Action Ready;
    }
}
