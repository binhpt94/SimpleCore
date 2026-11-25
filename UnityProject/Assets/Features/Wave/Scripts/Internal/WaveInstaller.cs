using System;
using Game.Core;
using Game.Enemy;
using Game.Run;
using Game.SimClock;
using Game.Wallet;
using UnityEngine;

namespace Game.Wave
{
    /// <summary>
    /// Wires Wave: builds the director from the config, publishes <see cref="IWaveProgress"/> (and, in dev builds,
    /// IWaveCheat) and joins the session.
    /// Install after SessionScene, SimClock, Run, Wallet and Enemy.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Wave/Wave Installer", fileName = "WaveInstaller")]
    public sealed class WaveInstaller : InstallerAsset
    {
        [SerializeField] private WaveConfig config;

        [Tooltip("Dev cheat (editor and development builds only): on the first step after the battle starts, jump to this wave " +
                 "and pause the run. 0 = off. Cheat state 'battle stands at wave 5, paused' uses 5.")]
        [Min(0)]
        [SerializeField] private int cheatPausedAtWave;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null) throw new InvalidOperationException($"{nameof(WaveInstaller)}: WaveConfig is not assigned.");

            var session = registry.Resolve<ISessionScene>();
            var run = registry.Resolve<IRunFlow>();
            var enemies = registry.Resolve<IEnemyField>();
            var income = registry.Resolve<IWalletIncome>();
            var clock = registry.Resolve<ISimClock>();
            var random = registry.Resolve<ISimRandom>();

            var stream = random.Stream(WaveParticipant.Owner);
            var director = new WaveDirector(config.ToRules(), run, enemies, income, new StreamWaveRandom(stream));
            registry.Register<IWaveProgress>(director);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            registry.Register<IWaveCheat>(director);
#endif
            session.Join(new WaveParticipant(director, clock));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (cheatPausedAtWave > 0)
                session.Join(new WaveStartCheat(director, run, cheatPausedAtWave));
#endif
        }
    }
}
