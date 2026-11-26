using System;
using System.Globalization;

namespace Game.Save
{
    /// <summary>
    /// Rules of feature-save/save-io/io-rules. The saved text is two lines: data version, then record.
    /// Read gives the record only for version 1 with an integer record; anything else is "none yet" and is
    /// left on the device untouched. A device holding a newer version refuses every write.
    /// Both calls are synchronous, so writes apply in call order.
    /// </summary>
    internal sealed class SavedRecord : ISavedRecord
    {
        /// <summary>data_version of the Params table.</summary>
        public const int DataVersion = 1;

        private readonly IRecordStore store;
        private readonly Action<string> warn;

        /// <param name="warn">Dev-only warning sink (null = silent). The player never sees a failed write.</param>
        public SavedRecord(IRecordStore store, Action<string> warn)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.warn = warn;
        }

        public bool TryRead(out int record)
        {
            record = 0;
            if (!store.TryLoad(out var text)) return false;
            if (!TryParse(text, out var version, out var value) || version != DataVersion) return false;
            record = value;
            return true;
        }

        public bool Write(int record)
        {
            if (store.TryLoad(out var existing) && TryParseVersion(existing, out var version) && version > DataVersion)
            {
                Warn($"refused: saved data has version {version}, newer than {DataVersion}");
                return false;
            }
            if (store.TrySave(Format(record), out var reason)) return true;
            Warn("device write failed: " + reason);
            return false;
        }

        internal static string Format(int record) =>
            DataVersion.ToString(CultureInfo.InvariantCulture) + "\n" + record.ToString(CultureInfo.InvariantCulture);

        private static bool TryParse(string text, out int version, out int record)
        {
            record = 0;
            if (!TryParseVersion(text, out version)) return false;
            var lines = Lines(text);
            return lines.Length == 2 && TryInt(lines[1], out record);
        }

        private static bool TryParseVersion(string text, out int version)
        {
            version = 0;
            if (text == null) return false;
            var lines = Lines(text);
            return lines.Length >= 1 && TryInt(lines[0], out version) && version > 0;
        }

        private static string[] Lines(string text) =>
            text.Replace("\r", "").TrimEnd('\n').Split('\n');

        private static bool TryInt(string s, out int value) =>
            int.TryParse(s.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);

        private void Warn(string message) => warn?.Invoke("[Save] write failed, " + message);
    }
}
