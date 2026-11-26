namespace Game.Save
{
    /// <summary>
    /// The one saved record of the game, kept on the device across restarts. Save only stores the number it is
    /// given and hands back that same number; what the number means (and its default) belongs to the caller.
    /// Both calls finish before they return. Usable from any scene.
    /// </summary>
    public interface ISavedRecord
    {
        /// <summary>
        /// Reads the saved record. Returns false ("none yet") when nothing was ever written, the saved data is
        /// unreadable, or it was written by a newer data version; record is then 0 and must not be used.
        /// Reading never deletes or rewrites what is on the device.
        /// </summary>
        bool TryRead(out int record);

        /// <summary>
        /// Replaces the whole saved record with this number. Returns true when it is fully on the device,
        /// false when the write failed or was refused; a failed write leaves the previous saved data intact.
        /// Writes are applied in the order they are called.
        /// </summary>
        bool Write(int record);
    }
}
