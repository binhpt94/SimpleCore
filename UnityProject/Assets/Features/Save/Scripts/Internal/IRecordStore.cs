namespace Game.Save
{
    /// <summary>Device port: holds the raw text of the one saved record. Swapped by a fake in tests.</summary>
    internal interface IRecordStore
    {
        /// <summary>False when nothing is stored (or it cannot be read); text is then null.</summary>
        bool TryLoad(out string text);

        /// <summary>Replaces the stored text as a whole. False (with a reason) when the device refused it; the previous text stays.</summary>
        bool TrySave(string text, out string reason);
    }
}
