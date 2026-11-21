using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The shared assets of one kind for one scene: id -> asset, for example the popups of the Game scene.
    /// Catalog assets live in _Composition and are listed on the scene's Boot. Which kinds exist and what
    /// an id looks like is up to the service that reads the catalog; Core knows no feature or service.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Asset Catalog")]
    public sealed class AssetCatalog : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public string id;
            public UnityEngine.Object asset;
        }

        [SerializeField] private string kind = "";
        [SerializeField] private Entry[] entries = new Entry[0];

        /// <summary>Which service reads this catalog, for example "Popup".</summary>
        public string Kind => kind;

        public IEnumerable<string> Ids
        {
            get
            {
                foreach (var entry in entries) yield return entry.id;
            }
        }

        public bool TryGet(string id, out UnityEngine.Object asset)
        {
            foreach (var entry in entries)
            {
                if (entry.id == id)
                {
                    asset = entry.asset;
                    return asset != null;
                }
            }
            asset = null;
            return false;
        }

        /// <summary>Throws when an id is empty or listed twice, or an entry has no asset. Message names the catalog and the id.</summary>
        public void Check()
        {
            var seen = new HashSet<string>();
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.id))
                    throw new InvalidOperationException($"Asset catalog '{name}' has an entry without an id.");
                if (!seen.Add(entry.id))
                    throw new InvalidOperationException($"Asset catalog '{name}' lists id '{entry.id}' twice.");
                if (entry.asset == null)
                    throw new InvalidOperationException($"Asset catalog '{name}': id '{entry.id}' has no asset.");
            }
        }
    }
}
