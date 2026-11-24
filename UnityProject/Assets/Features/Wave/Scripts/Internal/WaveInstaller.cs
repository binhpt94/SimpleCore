using System;
using Game.Core;
using Game.Enemy;
using Game.Run;
using Game.Wallet;
using UnityEngine;

namespace Game.Wave
{
    /// <summary>
    /// Wires Wave: builds the director from the config, publishes <see cref="IWaveProgress"/> and joins the session.
    /// Install after SessionScene, Run, Wallet and Enemy.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Wave/Wave Installer", fileName = "WaveInstaller")]
    public sealed class WaveInstaller : InstallerAsset
    {
        [SerializeField] private WaveConfig config;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null) throw new InvalidOperationException($"{nameof(WaveInstaller)}: WaveConfig is not assigned.");

            var session = registry.Resolve<ISessionScene>();
            var run = registry.Resolve<IRunFlow>();
            var enemies = registry.Resolve<IEnemyField>();
            var income = registry.Resolve<IWalletIncome>();

            var director = new WaveDirector(config.ToRules(), run, enemies, income, new SystemWaveRandom());
            registry.Register<IWaveProgress>(director);
            session.Join(new WaveParticipant(director, new UnityWaveClock()));
        }
    }
}
