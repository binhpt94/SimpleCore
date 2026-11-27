#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Every cheat state source the build boots, in every scene, with the scene that boots it. Boot registers it
    /// in every scene before any installer runs, built from the installers of each scene at the current stage, so
    /// a source the build does not boot is not listed. Dev builds only. No game rules.
    /// </summary>
    public interface ICheatStateCatalog
    {
        /// <summary>One entry per installer asset that is an <see cref="ICheatStateSource"/>. Never null; may be empty.</summary>
        IReadOnlyList<CheatStateSceneSource> Sources { get; }
    }

    /// <summary>A cheat state source and the scene whose boot installs it.</summary>
    public readonly struct CheatStateSceneSource
    {
        public CheatStateSceneSource(string scene, ICheatStateSource source)
        {
            Scene = scene;
            Source = source;
        }

        /// <summary>Name of the scene to load so that <see cref="Source"/> builds its state.</summary>
        public string Scene { get; }

        public ICheatStateSource Source { get; }
    }
}
#endif
