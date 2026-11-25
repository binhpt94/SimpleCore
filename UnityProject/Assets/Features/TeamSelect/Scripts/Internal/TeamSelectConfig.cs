using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.TeamSelect
{
    /// <summary>Tunable values of the team select screen; the asset lives in FeatureAssets/Private.</summary>
    [CreateAssetMenu(menuName = "Game/TeamSelect/Config", fileName = "TeamSelectConfig")]
    internal sealed class TeamSelectConfig : ScriptableObject
    {
        [Serializable]
        internal struct RoleWord
        {
            public string heroCode;
            public string word;
        }

        [Min(1)] public int slotCount = 3;

        [Tooltip("Team in the slots while no team is remembered, by hero code, slot 1 first.")]
        public string[] defaultTeam = { "knight", "axeman", "archer" };

        public string meleeWord = "Cận chiến";
        public string rangedWord = "Bắn xa";
        public string separator = " · ";

        [Tooltip("Role word of heroes whose attack kind alone does not tell them apart.")]
        public RoleWord[] roleWords =
        {
            new RoleWord { heroCode = "knight", word = "Chắn" },
            new RoleWord { heroCode = "swordsman", word = "Đánh nhanh" },
            new RoleWord { heroCode = "archer", word = "Tầm xa" },
        };

        public string singleTargetWord = "Một mục tiêu";
        public string frontCleaveWord = "Chém vùng";
        public string blastWord = "Nổ vùng";

        public TeamSelectRules ToRules()
        {
            var words = new Dictionary<string, string>();
            if (roleWords != null)
                foreach (var w in roleWords)
                    if (!string.IsNullOrEmpty(w.heroCode)) words[w.heroCode] = w.word;
            return new TeamSelectRules(slotCount, defaultTeam ?? Array.Empty<string>(), meleeWord, rangedWord, separator,
                words, singleTargetWord, frontCleaveWord, blastWord);
        }
    }
}
