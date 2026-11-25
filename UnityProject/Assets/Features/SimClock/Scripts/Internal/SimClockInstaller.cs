using System;
using Game.Core;
using UnityEngine;

namespace Game.SimClock
{
    /// <summary>
    /// Publishes ISimClock, ISimRunControl and ISimRandom and joins the scene session to drive the clock each frame.
    /// Install right after SessionScene and before every gameplay feature. Wiring only.
    /// </summary>
    public sealed class SimClockInstaller : InstallerAsset
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const bool DevBuild = true;
#else
        private const bool DevBuild = false;
#endif

        [SerializeField] private SimClockConfig config;

        internal SimClockConfig Config
        {
            get => config;
            set => config = value;
        }

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null)
                throw new InvalidOperationException(
                    $"{name}: the SimClock config (SimClockConfig in SimClock/FeatureAssets/Private) is not set.");
            config.Validate();
            var session = registry.Resolve<ISessionScene>();

            var clock = new SimClockService(config, DevBuild, new SystemSeedSource(), new UnitySeedLog());
            registry.Register<ISimClock>(clock);
            registry.Register<ISimRunControl>(clock);
            registry.Register<ISimRandom>(clock);
            session.Join(new SimClockParticipant(clock, new UnityFrameTime()));
        }
    }
}
