#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.CheatPanel
{
    /// <summary>
    /// Dev builds only: the states of every cheat state source the build boots (Core's catalog, so a state whose code
    /// the build lacks is not there) and the shared in-memory selection. Writes nothing to the device.
    /// </summary>
    internal sealed class CatalogCheatStates : ICheatStates
    {
        private readonly ICheatSelection _selection;

        public CatalogCheatStates(ICheatStateCatalog catalog, ICheatSelection selection)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            var entries = new List<CheatEntry>();
            foreach (var source in catalog.Sources)
            {
                if (source.Source == null) continue;
                foreach (var state in source.Source.CheatStates)
                    entries.Add(new CheatEntry(source.Source.CheatFeatureTitle, state.Key, state.Title, source.Scene));
            }
            Entries = entries;
        }

        public IReadOnlyList<CheatEntry> Entries { get; }

        public string RunningKey => _selection.RunningKey;

        public void Pick(string key) => _selection.Pick(key);
    }
}
#endif
