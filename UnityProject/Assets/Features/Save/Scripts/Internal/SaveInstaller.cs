using System;
using System.IO;
using Game.Core;
using UnityEngine;

namespace Game.Save
{
    /// <summary>
    /// Wires Save: publishes <see cref="ISavedRecord"/> backed by one file in Application.persistentDataPath.
    /// No feature dependencies, no view, no session participant. Dev builds log failed writes as warnings and,
    /// when Core's ICheatSelection is registered, skip writes while a cheat state runs.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Save/Save Installer", fileName = "SaveInstaller")]
    public sealed class SaveInstaller : InstallerAsset
    {
        internal const string FileName = "record.save";

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            var store = new FileRecordStore(Path.Combine(Application.persistentDataPath, FileName));
            ISavedRecord saved = new SavedRecord(store, DevWarning());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // apply-no-save: while a cheat state runs, writes are skipped (reported as success).
            if (registry.TryResolve<ICheatSelection>(out var cheats)) saved = new CheatSafeSavedRecord(saved, cheats);
#endif
            registry.Register(saved);
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
