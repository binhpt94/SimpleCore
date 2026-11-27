#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Game.Core;

namespace Game.Save
{
    /// <summary>
    /// Dev builds only (feature-cheat-panel/cheat-states/states-rules#apply-no-save): while a cheat state is running
    /// (<see cref="ICheatSelection.RunningKey"/> not null) every write is skipped but reported as a success, so the
    /// record forced by the state never overwrites the real record on the device and the player sees no error.
    /// Reads pass through. Release builds do not contain this class.
    /// </summary>
    internal sealed class CheatSafeSavedRecord : ISavedRecord
    {
        private readonly ISavedRecord inner;
        private readonly ICheatSelection cheats;

        public CheatSafeSavedRecord(ISavedRecord inner, ICheatSelection cheats)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.cheats = cheats ?? throw new ArgumentNullException(nameof(cheats));
        }

        public bool TryRead(out int record) => inner.TryRead(out record);

        public bool Write(int record)
        {
            if (cheats.RunningKey != null) return true;
            return inner.Write(record);
        }
    }
}
#endif
