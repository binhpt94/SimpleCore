using System;
using Game.Core;
using UnityEngine;

namespace Game.Services.SessionScene
{
    [CreateAssetMenu(menuName = "Game/Installers/SessionScene")]
    public sealed class SessionSceneInstaller : InstallerAsset
    {
        [SerializeField] private GameObject driverPrefab;

        [Tooltip("Longest one participant may take to load before the session fails, in seconds.")]
        [SerializeField] private float loadTimeLimitSeconds = 10f;

        public override void Install(IServiceRegistry registry)
        {
            if (driverPrefab == null)
            {
                throw new InvalidOperationException(
                    "SessionSceneInstaller: driverPrefab is not assigned; without it the session never leaves Init.");
            }

            if (loadTimeLimitSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    $"SessionSceneInstaller: loadTimeLimitSeconds must be positive, it is {loadTimeLimitSeconds}.");
            }

            // The clean-up check counts what participants hold only in the Editor and development builds.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            IParticipantUsage usage = new ParticipantUsageCounter();
#else
            IParticipantUsage usage = null;
#endif
            var logic = new SessionSceneLogic(
                Debug.LogException, Debug.LogWarning, new FrameLoadTimer(), TimeSpan.FromSeconds(loadTimeLimitSeconds),
                usage);
            registry.Register<ISessionScene>(logic);
            registry.Register<ISessionLoadProgress>(logic);

            // Outside Play mode (EditMode tests run installers) no Start would ever come, and the
            // driver object would be left behind in the open scene.
            if (Application.isPlaying)
            {
                new PrefabSessionDriverSource(driverPrefab).Create().Drive(logic);
            }
        }
    }
}
