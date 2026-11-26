using System;
using System.IO;
using Game.Core;
using UnityEngine;

namespace Game.Save
{
    /// <summary>
    /// Wires Save: publishes <see cref="ISavedRecord"/> backed by one file in Application.persistentDataPath.
    /// No dependencies, no view, no session participant. Dev builds log failed writes as warnings.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Save/Save Installer", fileName = "SaveInstaller")]
    public sealed class SaveInstaller : InstallerAsset
    {
        internal const string FileName = "record.save";

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            var store = new FileRecordStore(Path.Combine(Application.persistentDataPath, FileName));
            registry.Register<ISavedRecord>(new SavedRecord(store, DevWarning()));
        }

        private static Action<string> DevWarning()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return Debug.LogWarning;
#else
            return null;
#endif
        }
    }
}
