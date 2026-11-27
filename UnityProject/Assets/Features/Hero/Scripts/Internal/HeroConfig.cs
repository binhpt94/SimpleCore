using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Balance numbers of the hero feature (live: feature-hero/hero-list/hero-roster-values,
    /// hero-respawn/respawn-values, hero-stats/hero-level-values). Every number lives in the asset, not in code.
    /// </summary>
    internal sealed class HeroConfig : ScriptableObject
    {
        /// <summary>Development stage a hero or attack kind is in the game from (column `stage` of the design tables).</summary>
        internal enum Stage
        {
            Idea = 0,
            Prototype = 1,
            Full = 2,
        }

        /// <summary>One hero of the roster at level 1.</summary>
        [Serializable]
        internal sealed class HeroEntry
        {
            [Tooltip("Stable key, for example knight.")]
            public string code;
            public string displayName;
            [Tooltip("First development stage this hero is in the roster.")]
            public Stage stage;
            public HeroGroup group;
            public HeroAttack attack;
            [Tooltip("Max health at level 1.")]
            public float maxHealth;
            [Tooltip("Damage of one hit at level 1.")]
            public float damage;
            [Tooltip("Seconds between two hits at level 1.")]
            public float secondsPerAttack;
            [Tooltip("Attack range in u, measured between box edges.")]
            public float range;
            [Tooltip("Walk speed in u/s.")]
            public float moveSpeed;
            [Tooltip("FrontCleave: length of the cleave from the hero's front edge. Blast: radius around the target's centre. SingleTarget: unused.")]
            public float areaSize;
            [Tooltip("GreyBox colour when art Hero/<code> has no file. The sprite comes from Core's IArt, drawn facing right.")]
            public Color placeholderColor = Color.white;
        }

        [SerializeField] private List<HeroEntry> heroes = new List<HeroEntry>();

        [Tooltip("Prefab of one hero on the battlefield; its root has the hero view.")]
        [SerializeField] private GameObject heroPrefab;

        [Tooltip("Heroes only target enemies whose centre is within ±this x (u): the visible screen.")]
        [SerializeField] private float targetHalfRange;

        [Tooltip("A hero's centre never goes beyond ±this x (u).")]
        [SerializeField] private float walkHalfRange;

        [Tooltip("Spawn and respawn x (u) of each team slot, slot 1 first.")]
        [SerializeField] private float[] slotSpawnX = Array.Empty<float>();

        [Tooltip("Hero box width in u (every hero shares the same box).")]
        [SerializeField] private float boxWidth;

        [Tooltip("Hero box height in u.")]
        [SerializeField] private float boxHeight;

        [Tooltip("Seconds a fallen hero waits before it respawns.")]
        [SerializeField] private float respawnSeconds;

        [Tooltip("Seconds a falling hero shrinks to nothing (visual only).")]
        [SerializeField] private float fallSeconds;

        [Tooltip("Seconds a respawning hero grows back; it neither hits nor is hit meanwhile.")]
        [SerializeField] private float respawnGrowSeconds;

        [Tooltip("Max health and damage gained per level, as a share of level 1.")]
        [SerializeField] private float levelStatStep;

        [Tooltip("Attack speed gained per level, as a share of level 1.")]
        [SerializeField] private float levelAttackSpeedStep;

        [Tooltip("Highest total attack speed gain from levels.")]
        [SerializeField] private float levelAttackSpeedCap;

        [Tooltip("Level-up pop: the hero grows to this scale and back.")]
        [SerializeField] private float levelUpPopScale;

        [Tooltip("Level-up pop: seconds from start back to normal size.")]
        [SerializeField] private float levelUpPopSeconds;

        [Tooltip("Seconds the new level number shows above the hero after a level up (PROTOTYPE).")]
        [SerializeField] private float levelUpLabelSeconds;

        public IReadOnlyList<HeroEntry> Heroes => heroes;
        public GameObject HeroPrefab => heroPrefab;
        public float TargetHalfRange => targetHalfRange;
        public float WalkHalfRange => walkHalfRange;
        public IReadOnlyList<float> SlotSpawnX => slotSpawnX;
        public float BoxWidth => boxWidth;
        public float BoxHeight => boxHeight;
        public float RespawnSeconds => respawnSeconds;
        public float FallSeconds => fallSeconds;
        public float RespawnGrowSeconds => respawnGrowSeconds;
        public float LevelStatStep => levelStatStep;
        public float LevelAttackSpeedStep => levelAttackSpeedStep;
        public float LevelAttackSpeedCap => levelAttackSpeedCap;
        public float LevelUpPopScale => levelUpPopScale;
        public float LevelUpPopSeconds => levelUpPopSeconds;
        public float LevelUpLabelSeconds => levelUpLabelSeconds;

        /// <summary>Heroes in the game at <paramref name="stage"/>: those whose own stage is that one or earlier, in roster order.</summary>
        public List<HeroEntry> HeroesAt(Stage stage)
        {
            var result = new List<HeroEntry>();
            if (heroes == null) return result;
            foreach (var hero in heroes)
                if (hero != null && hero.stage <= stage) result.Add(hero);
            return result;
        }

        public HeroEntry Find(string code)
        {
            if (heroes == null) return null;
            foreach (var hero in heroes)
                if (hero != null && hero.code == code) return hero;
            return null;
        }

        /// <summary>Null when the asset is usable; otherwise what is wrong with it, for the Install error.</summary>
        public string FindProblem()
        {
            if (heroes == null || heroes.Count == 0) return "no heroes";
            var codes = new HashSet<string>();
            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero == null) return $"hero {i} is empty";
                if (string.IsNullOrEmpty(hero.code)) return $"hero {i} has no code";
                if (!codes.Add(hero.code)) return $"hero code '{hero.code}' appears twice";
                if (hero.maxHealth <= 0f) return $"hero '{hero.code}' has no max health";
                if (hero.damage < 0f) return $"hero '{hero.code}' has negative damage";
                if (hero.secondsPerAttack <= 0f) return $"hero '{hero.code}' has no seconds per attack";
                if (hero.range < 0f) return $"hero '{hero.code}' has negative range";
                if (hero.moveSpeed < 0f) return $"hero '{hero.code}' has negative move speed";
                if (hero.attack != HeroAttack.SingleTarget && hero.areaSize <= 0f)
                    return $"hero '{hero.code}' has an area attack without area size";
            }
            if (slotSpawnX == null || slotSpawnX.Length == 0) return "no slot spawn points";
            if (boxWidth <= 0f || boxHeight <= 0f) return "hero box has no size";
            if (targetHalfRange <= 0f || walkHalfRange <= 0f) return "no target or walk range";
            if (respawnSeconds < 0f || fallSeconds < 0f || respawnGrowSeconds < 0f) return "negative respawn timing";
            if (levelStatStep < 0f || levelAttackSpeedStep < 0f || levelAttackSpeedCap < 0f) return "negative level step";
            if (levelUpPopScale < 1f || levelUpPopSeconds <= 0f) return "no level-up pop scale or seconds";
            if (levelUpLabelSeconds < 0f) return "negative level-up label seconds";
            return null;
        }
    }
}
