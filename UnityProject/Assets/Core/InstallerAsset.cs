using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Base of every feature installer. The installer is the one public type of a feature's Internal
    /// assembly and lives as an asset in FeatureAssets/Public, so _Composition can list it without
    /// touching the feature's private assets or types.
    /// </summary>
    public abstract class InstallerAsset : ScriptableObject
    {
        public abstract void Install(IServiceRegistry registry);
    }
}
