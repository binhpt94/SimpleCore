using UnityEngine;

namespace Game.Hud
{
    /// <summary>The HUD's tuning numbers (feature-hud/hud-status/status-spec, feature-hud/hud-skill-button/skill-button-spec). Read only at runtime.</summary>
    [CreateAssetMenu(menuName = "Game/Hud/Config", fileName = "HudConfig")]
    internal sealed class HudConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float bannerSeconds = 1.5f;
        [SerializeField, Min(1f)] private float bounceScale = 1.2f;
        [SerializeField, Min(0f)] private float bounceSeconds = 0.3f;

        [Tooltip("feature-hud/hud-pause-button/pause-button-spec, 'Có nút tạm dừng': IDEA no, PROTOTYPE optional, FULL yes.")]
        [SerializeField] private bool showPauseButton;

        public HudTimings Timings => new HudTimings(bannerSeconds, bounceScale, bounceSeconds);

        /// <summary>Whether the battle HUD has its pause button at this stage of the game.</summary>
        public bool ShowsPauseButton => showPauseButton;
    }
}
