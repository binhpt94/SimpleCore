using UnityEngine;

namespace Game.Castle
{
    /// <summary>
    /// The castle's balance numbers (live/feature-castle, round 4). Lengths in battlefield units (u),
    /// times in seconds. Level bonuses are fractions of the level-1 value added per level above 1.
    /// </summary>
    internal sealed class CastleConfig : ScriptableObject
    {
        [Header("Body (castle-body-values)")]
        [SerializeField] private float maxHealth = 1000f;
        [SerializeField] private float halfWidth = 1.5f;
        [SerializeField] private float healthPerLevel = 150f;
        [Tooltip("Revive: current health set to this fraction of max health at that moment, rounded up.")]
        [SerializeField] private float reviveFraction = 0.3f;
        [Tooltip("Hero start and revive x (u) by team slot: slot 1 and 3 at the left edge, slot 2 at the right edge.")]
        [SerializeField] private float[] heroSlotX = { -1.5f, 1.5f, -1.5f };

        [Header("Auto shot (auto-shot-values)")]
        [SerializeField] private float shotDamage = 10f;
        [SerializeField] private float secondsPerShot = 1f;
        [SerializeField] private float shotRange = 6f;
        [SerializeField] private float screenHalfWidth = 16f;
        [SerializeField] private float attackBonusPerLevel = 0.2f;

        [Header("Skills (skill-values)")]
        [SerializeField] private CastleSkill[] skills = { CastleSkill.Thunder, CastleSkill.Frost, CastleSkill.Rally };
        [SerializeField] private float thunderDamage = 80f;
        [SerializeField] private float thunderRange = 6f;
        [SerializeField] private float thunderCooldown = 20f;
        [SerializeField] private float frostSeconds = 5f;
        [SerializeField] private float frostCooldown = 35f;
        [SerializeField] private float rallyFraction = 0.3f;
        [SerializeField] private float rallyCooldown = 25f;
        [SerializeField] private float skillBonusPerLevel = 0.2f;

        [Header("View")]
        [SerializeField] private GameObject viewPrefab;
        [SerializeField] private float height = 4f;
        [SerializeField] private float hitFlashSeconds = 0.1f;
        [Tooltip("PROTOTYPE: a hit also shakes the castle lightly for this long (0 = no shake).")]
        [SerializeField] private float hitShakeSeconds = 0.1f;
        [Tooltip("PROTOTYPE: below this fraction of max health the castle shows its damaged picture.")]
        [SerializeField] private float damagedHealthFraction = 0.5f;
        [SerializeField] private float collapseSeconds = 0.5f;
        [Tooltip("Revive: grows back from 0 to full size over this time (the fall reversed).")]
        [SerializeField] private float reviveGrowSeconds = 0.5f;
        [SerializeField] private float shotFlightSeconds = 0.15f;
        [SerializeField] private float thunderEffectSeconds = 0.3f;
        [Tooltip("Level-up pop (feature-upgrade/upgrade-effect/effect-numbers, IDEA: grow then back in 0.3s).")]
        [SerializeField] private float levelUpPopScale = 1.1f;
        [SerializeField] private float levelUpPopSeconds = 0.3f;

        public float MaxHealth => maxHealth;
        public float HalfWidth => halfWidth;
        public float HealthPerLevel => healthPerLevel;
        public float ReviveFraction => reviveFraction;
        public float[] HeroSlotX => heroSlotX;

        public float ShotDamage => shotDamage;
        public float SecondsPerShot => secondsPerShot;
        public float ShotRange => shotRange;
        public float ScreenHalfWidth => screenHalfWidth;
        public float AttackBonusPerLevel => attackBonusPerLevel;

        public CastleSkill[] Skills => skills;
        public float ThunderDamage => thunderDamage;
        public float ThunderRange => thunderRange;
        public float ThunderCooldown => thunderCooldown;
        public float FrostSeconds => frostSeconds;
        public float FrostCooldown => frostCooldown;
        public float RallyFraction => rallyFraction;
        public float RallyCooldown => rallyCooldown;
        public float SkillBonusPerLevel => skillBonusPerLevel;

        public GameObject ViewPrefab => viewPrefab;
        public float Height => height;
        public float HitFlashSeconds => hitFlashSeconds;
        public float HitShakeSeconds => hitShakeSeconds;
        public float DamagedHealthFraction => damagedHealthFraction;
        public float CollapseSeconds => collapseSeconds;
        public float ReviveGrowSeconds => reviveGrowSeconds;
        public float ShotFlightSeconds => shotFlightSeconds;
        public float ThunderEffectSeconds => thunderEffectSeconds;
        public float LevelUpPopScale => levelUpPopScale;
        public float LevelUpPopSeconds => levelUpPopSeconds;
    }
}
