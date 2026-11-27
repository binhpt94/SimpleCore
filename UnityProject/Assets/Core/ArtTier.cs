namespace Game.Core
{
    /// <summary>The three art levels of a key: GreyBox (no file), Simple (AI-made file), Detail (the person's file).</summary>
    public enum ArtTier
    {
        GreyBox,
        Simple,
        Detail,
    }

    /// <summary>Pure rule that picks the art level of one key. No Unity, no files.</summary>
    public static class ArtTiers
    {
        /// <summary>
        /// Detail when it exists and the person did not keep the key on Simple (<paramref name="keptSimple"/>);
        /// otherwise Simple when it exists; otherwise GreyBox. A kept key without a Simple file shows Detail.
        /// </summary>
        public static ArtTier Pick(bool hasDetail, bool hasSimple, bool keptSimple)
        {
            if (hasDetail && (!keptSimple || !hasSimple)) return ArtTier.Detail;
            if (hasSimple) return ArtTier.Simple;
            return ArtTier.GreyBox;
        }
    }
}
