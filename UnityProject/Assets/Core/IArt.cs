using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The art of a key <c>F/id</c> (feature F, art id). <see cref="ArtInstaller"/> registers it; a feature resolves it in
    /// Install and keeps it. The level follows <see cref="ArtTiers.Pick"/>; with no file the answer is <see cref="GreyBox"/>.
    /// Every call makes a new object; the caller owns it, reuses it and destroys it (a Sprite file is returned as is).
    /// Throws <see cref="System.ArgumentException"/> when the key is not of the form F/id.
    /// </summary>
    public interface IArt
    {
        /// <summary>The 2D art of the key: the Sprite file, a Texture2D file as a centred sprite <c>look.Size</c> wide, or <see cref="GreyBox.Sprite"/>.</summary>
        Sprite Sprite(string key, GreyBox.Look look);

        /// <summary>A new instance of the key's model file, or a <see cref="GreyBox.Model"/> cube of <paramref name="size"/>.</summary>
        GameObject Model(string key, Vector3 size);

        /// <summary>The level the key shows.</summary>
        ArtTier TierOf(string key);
    }
}
