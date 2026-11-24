using UnityEngine;

namespace Game.Upgrade
{
    /// <summary>Balance numbers of the upgrade shop (feature-upgrade/upgrade-purchase/purchase-rules). Read only at run time.</summary>
    [CreateAssetMenu(menuName = "Game/Upgrade/Config", fileName = "UpgradeConfig")]
    internal sealed class UpgradeConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int _teamSize = 3;

        [Header("Price of level L to L+1 = round up (base × growth^(L − 1))")]
        [SerializeField, Min(1)] private int _heroBase = 30;
        [SerializeField, Min(1f)] private double _heroGrowth = 1.35;
        [SerializeField, Min(1)] private int _castleHealthBase = 40;
        [SerializeField, Min(1f)] private double _castleHealthGrowth = 1.4;
        [SerializeField, Min(1)] private int _castleAttackBase = 40;
        [SerializeField, Min(1f)] private double _castleAttackGrowth = 1.4;
        [SerializeField, Min(1)] private int _castleSkillsBase = 60;
        [SerializeField, Min(1f)] private double _castleSkillsGrowth = 1.5;

        public UpgradePrices ToPrices() => new UpgradePrices(
            _teamSize,
            new PriceCurve(_heroBase, _heroGrowth),
            new PriceCurve(_castleHealthBase, _castleHealthGrowth),
            new PriceCurve(_castleAttackBase, _castleAttackGrowth),
            new PriceCurve(_castleSkillsBase, _castleSkillsGrowth));
    }
}
