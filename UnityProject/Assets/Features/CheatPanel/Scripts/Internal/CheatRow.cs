using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.CheatPanel
{
    /// <summary>One row of the cheat panel: the normal-play row (no state) or one state row.</summary>
    internal readonly struct CheatRow
    {
        public CheatRow(string label, string stateKey, string scene)
        {
            Label = label;
            StateKey = stateKey;
            Scene = scene;
        }

        public string Label { get; }

        /// <summary>Key of the row's state; null on the normal-play row.</summary>
        public string StateKey { get; }

        /// <summary>Scene to reload for the row's state; null on the normal-play row.</summary>
        public string Scene { get; }

        public bool IsNormal => StateKey == null;
    }

    /// <summary>Rules of node feature-cheat-panel/cheat-states/states-rules for the rows: order and text.</summary>
    internal static class CheatRows
    {
        /// <summary>
        /// "Chơi bình thường" first, then the state rows by feature title A to Z; within a feature, the feature's own
        /// order. Each state row reads "&lt;feature title&gt;: &lt;state title&gt;". Only states the build can build are given, so
        /// a state the build cannot build has no row.
        /// </summary>
        public static IReadOnlyList<CheatRow> Build(string normalRowLabel, IReadOnlyList<CheatEntry> entries)
        {
            var rows = new List<CheatRow> { new CheatRow(normalRowLabel, null, null) };
            if (entries == null) return rows;
            var features = entries.Select(e => e.FeatureTitle).Distinct()
                .OrderBy(t => t ?? string.Empty, StringComparer.InvariantCultureIgnoreCase).ToList();
            foreach (var feature in features)
                foreach (var e in entries.Where(e => e.FeatureTitle == feature && !string.IsNullOrEmpty(e.Key)))
                    rows.Add(new CheatRow($"{e.FeatureTitle}: {e.Title}", e.Key, e.Scene));
            return rows;
        }
    }
}
