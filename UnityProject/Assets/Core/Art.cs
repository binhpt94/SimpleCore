using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Core
{
    /// <summary>
    /// Art of one key <c>F/id</c>: files at Resources/Art/F/id/Simple and /Detail. Picks the level with
    /// <see cref="ArtTiers.Pick"/> and the person's <see cref="ArtChoice"/>, falls back to <see cref="GreyBox"/>.
    /// Static and stateless: reads files on every call, caches nothing.
    /// </summary>
    public static class Art
    {
        internal const string ChoicePath = "ArtChoice";

        /// <summary>The level the key would show with these files.</summary>
        internal static ArtTier TierOf(string key, IArtFiles files)
        {
            CheckKey(key);
            var choice = files.Load(ChoicePath) as TextAsset;
            var kept = ArtChoice.Parse(choice != null ? choice.text : null);
            var hasDetail = files.Load(PathOf(key, ArtTier.Detail)) != null;
            var hasSimple = files.Load(PathOf(key, ArtTier.Simple)) != null;
            return ArtTiers.Pick(hasDetail, hasSimple, kept.Contains(key));
        }

        /// <summary>
        /// The sprite of the key. A Sprite file is returned as is; a Texture2D file becomes a centred sprite
        /// <c>look.Size</c> units wide; no usable file gives <see cref="GreyBox.Sprite"/>.
        /// </summary>
        internal static Sprite Sprite(string key, GreyBox.Look look, IArtFiles files)
        {
            var file = FileOf(key, files);
            if (file is Sprite sprite) return sprite;
            if (file is Texture2D texture)
            {
                var made = UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), texture.width / look.Side);
                made.name = key;
                return made;
            }

            return GreyBox.Sprite(look);
        }

        /// <summary>
        /// A new instance of the key's model (a GameObject file, for example an imported .fbx), or a
        /// <see cref="GreyBox.Model"/> cube of <paramref name="size"/> when there is none. The caller destroys it.
        /// </summary>
        internal static GameObject Model(string key, Vector3 size, IArtFiles files)
        {
            if (FileOf(key, files) is GameObject prefab)
            {
                var model = Object.Instantiate(prefab);
                model.name = key;
                return model;
            }

            return GreyBox.Model(size);
        }

        internal static string PathOf(string key, ArtTier tier) => $"Art/{key}/{tier}";

        private static Object FileOf(string key, IArtFiles files)
        {
            var tier = TierOf(key, files);
            return tier == ArtTier.GreyBox ? null : files.Load(PathOf(key, tier));
        }

        private static void CheckKey(string key)
        {
            var slash = key?.IndexOf('/') ?? -1;
            if (slash <= 0 || slash == key.Length - 1 || key.IndexOf('/', slash + 1) >= 0)
                throw new ArgumentException($"Art key '{key}' is not of the form F/id.", nameof(key));
        }
    }
}
