using System;
using Game.Core;
using Game.Run;
using Game.SimClock;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Publishes <see cref="ICombat"/> and joins the scene session. Installed after SimClock and Run, before the
    /// features that deal hits (Enemy, Hero, Castle), which resolve <see cref="ICombat"/> when they install.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Combat/Combat Installer", fileName = "CombatInstaller")]
    public sealed class CombatInstaller : InstallerAsset
    {
        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            var session = Need<ISessionScene>(registry, "SessionScene");
            var clock = Need<ISimClock>(registry, "SimClock");
            var run = Need<IRunFlow>(registry, "Run");

            var tick = new CombatTick();
            registry.Register<ICombat>(tick);
            session.Join(new CombatParticipant(tick, clock, run));
        }

        private static T Need<T>(IServiceRegistry registry, string feature) where T : class
        {
            if (registry.TryResolve<T>(out var service)) return service;
            throw new InvalidOperationException(
                $"Combat: {typeof(T).Name} is missing. Install {feature} before Combat.");
        }
    }
}
