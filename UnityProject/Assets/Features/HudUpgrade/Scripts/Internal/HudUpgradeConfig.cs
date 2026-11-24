using UnityEngine;

namespace Game.HudUpgrade
{
    /// <summary>Dev-only states of the upgrade bar (design node feature-hud-upgrade/cheat).</summary>
    internal enum HudUpgradeCheatState
    {
        None,

        /// <summary>Mid battle: coins 120, levels 3, 1, 5, 2, 4, 3; five buttons lit, the skill button dim.</summary>
        MixedButtons,
    }

    /// <summary>Settings of the upgrade bar. Lives in FeatureAssets/Private; read only while playing.</summary>
    internal sealed class HudUpgradeConfig : ScriptableObject
    {
        [Tooltip("Chỉ có tác dụng ở bản dev (Editor hoặc Development Build).")]
        [SerializeField] private HudUpgradeCheatState cheatState = HudUpgradeCheatState.None;

        [Tooltip("mixed-buttons: số tiền sau khi ép.")]
        [SerializeField] private int mixedButtonsCoins = 120;

        [Tooltip("mixed-buttons: cấp của từng mục theo thứ tự thanh (hero 1, 2, 3, máu, sức đánh, kĩ năng).")]
        [SerializeField] private int[] mixedButtonsLevels = { 3, 1, 5, 2, 4, 3 };

        public HudUpgradeCheatState CheatState => cheatState;

        public int MixedButtonsCoins => mixedButtonsCoins;

        public int[] MixedButtonsLevels => mixedButtonsLevels;
    }
}
