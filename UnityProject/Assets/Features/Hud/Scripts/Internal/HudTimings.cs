namespace Game.Hud
{
    /// <summary>How long the HUD's short effects last. Read from <see cref="HudConfig"/>.</summary>
    internal readonly struct HudTimings
    {
        public HudTimings(float bannerSeconds, float bounceScale, float bounceSeconds)
        {
            BannerSeconds = bannerSeconds;
            BounceScale = bounceScale;
            BounceSeconds = bounceSeconds;
        }

        /// <summary>How long the "Đợt N" banner stays up after a wave starts.</summary>
        public float BannerSeconds { get; }

        /// <summary>Peak size of a skill button when its cooldown ends (1 = no bounce).</summary>
        public float BounceScale { get; }

        /// <summary>How long that bounce takes, there and back.</summary>
        public float BounceSeconds { get; }
    }
}
