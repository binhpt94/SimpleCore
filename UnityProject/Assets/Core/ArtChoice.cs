using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Reads the person's art choice, the TextAsset of id <c>ArtChoice</c> in the Art catalog (_Composition/ArtChoice.json): <c>{"simple": ["F/id", ...]}</c>, the keys
    /// that stay on Simple even when a Detail file exists. Pure: takes the text, holds nothing.
    /// </summary>
    public static class ArtChoice
    {
        /// <summary>Keys kept on Simple. Null, empty or broken JSON gives an empty set; empty keys are skipped.</summary>
        public static ISet<string> Parse(string json)
        {
            var kept = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(json)) return kept;
            Data data;
            try
            {
                data = JsonUtility.FromJson<Data>(json);
            }
            catch (ArgumentException)
            {
                return kept;
            }

            if (data?.simple == null) return kept;
            foreach (var key in data.simple)
            {
                if (!string.IsNullOrEmpty(key)) kept.Add(key);
            }

            return kept;
        }

        [Serializable]
        private sealed class Data
        {
            public string[] simple;
        }
    }
}
