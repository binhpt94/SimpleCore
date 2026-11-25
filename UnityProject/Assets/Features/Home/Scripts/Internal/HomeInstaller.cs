using System;
using Game.Core;
using Game.Run;
using UnityEngine;

namespace Game.Home
{
    /// <summary>Wires the start screen into the Game scene session. Needs IRunFlow, IRunScore and ISessionScene installed first.</summary>
    [CreateAssetMenu(menuName = "Game/Home/Home Installer")]
    public sealed class HomeInstaller : InstallerAsset
    {
        [SerializeField] private HomeConfig config;
        [SerializeField] private HomeScreenFactoryAsset screenFactory;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Tooltip("Dev only: a state of feature-home/cheat to build at start.")]
        [SerializeField] private HomeCheatState cheat = HomeCheatState.None;
#endif

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (config == null) throw new InvalidOperationException($"{name}: Home config is missing.");
            if (screenFactory == null) throw new InvalidOperationException($"{name}: Home screen factory is missing.");
            config.Check();

            var flow = Need<IRunFlow>(registry);
            var score = Need<IRunScore>(registry);
            var session = Need<ISessionScene>(registry);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            score = HomeCheat.Apply(cheat, score);
#endif
            session.Join(new HomeParticipant(flow, score, config, screenFactory));
        }

        private T Need<T>(IServiceRegistry registry) where T : class
        {
            if (registry.TryResolve<T>(out var service)) return service;
            throw new InvalidOperationException($"{name}: {typeof(T).Name} is not installed; install its feature before Home.");
        }
    }
}
