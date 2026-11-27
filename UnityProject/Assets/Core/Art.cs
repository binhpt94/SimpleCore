using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Core
{
    /// <summary>
    /// <see cref="IArt"/> read from the scene's catalog of kind <see cref="Kind"/>: id <c>F/id/Simple</c> and
    /// <c>F/id/Detail</c> for the files, id <see cref="ChoiceId"/> for the person's <see cref="ArtChoice"/> TextAsset.
    /// No catalog or no ArtChoice counts as empty: everything without a file is GreyBox, nothing throws.
    /// </summary>
    internal sealed class Art : IArt
    {
        internal const string Kind = "Art";
        internal const string ChoiceId = "ArtChoice";

        private readonly AssetCatalog catalog;
        private readonly ISet<string> kept;

        /// <param name="catalog">The Art catalog of the scene, or null when the scene has none.</param>
        internal Art(AssetCatalog catalog)
        {
            this.catalog = catalog;
            var choice = Find(ChoiceId) as TextAsset;
            kept = ArtChoice.Parse(choice != null ? choice.text : null);
        }

        internal static string IdOf(string key, ArtTier tier) => $"{key}/{tier}";

        public ArtTier TierOf(string key)
        {
            CheckKey(key);
            return ArtTiers.Pick(Find(IdOf(key, ArtTier.Detail)) != null, Find(IdOf(key, ArtTier.Simple)) != null,
                kept.Contains(key));
        }

        public Sprite Sprite(string key, GreyBox.Look look)
        {
            var file = FileOf(key);
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

        public GameObject Model(string key, Vector3 size)
        {
            if (FileOf(key) is GameObject prefab)
            {
                var model = Object.Instantiate(prefab);
                model.name = key;
                return model;
            }

            return GreyBox.Model(size);
        }

        private Object FileOf(string key)
        {
            var tier = TierOf(key);
            return tier == ArtTier.GreyBox ? null : Find(IdOf(key, tier));
        }

        private Object Find(string id) => catalog != null && catalog.TryGet(id, out var asset) ? asset : null;

        private static void CheckKey(string key)
        {
            var slash = key?.IndexOf('/') ?? -1;
            if (slash <= 0 || slash == key.Length - 1 || key.IndexOf('/', slash + 1) >= 0)
                throw new ArgumentException($"Art key '{key}' is not of the form F/id.", nameof(key));
        }
    }
}
