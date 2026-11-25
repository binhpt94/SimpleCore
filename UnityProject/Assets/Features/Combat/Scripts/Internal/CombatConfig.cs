using UnityEngine;

namespace Game.Combat
{
    /// <summary>Combat balance asset, kept in FeatureAssets/Private.</summary>
    [CreateAssetMenu(menuName = "Game/Combat/Combat Config", fileName = "CombatConfig")]
    internal sealed class CombatConfig : ScriptableObject
    {
        [Tooltip("Length of one fixed battle step and combat tick, in seconds (0.02 = 50 steps per second).")]
        [SerializeField] private float stepSeconds = 0.02f;

        [Tooltip("Most steps run in one frame; time beyond it is dropped so a slow device slows the battle down.")]
        [SerializeField] private int maxStepsPerFrame = 5;

        public CombatSettings ToSettings() => new CombatSettings(stepSeconds, maxStepsPerFrame);
    }
}
