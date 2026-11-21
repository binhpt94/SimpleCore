using System;
using Game.Core;
using UnityEngine;

namespace Game.Services.SessionScene
{
    [CreateAssetMenu(menuName = "Game/Installers/SessionScene")]
    public sealed class SessionSceneInstaller : InstallerAsset
    {
        [SerializeField] private GameObject driverPrefab;

        public override void Install(IServiceRegistry registry)
        {
            if (driverPrefab == null)
            {
                throw new InvalidOperationException(
                    "SessionSceneInstaller: driverPrefab is not assigned; without it the session never leaves Init.");
            }

            var logic = new SessionSceneLogic(Debug.LogException);
            registry.Register<ISessionScene>(logic);

            // Outside Play mode (EditMode tests run installers) no Start would ever come, and the
            // driver object would be left behind in the open scene.
            if (Application.isPlaying)
            {
                new PrefabSessionDriverSource(driverPrefab).Create().Drive(logic);
            }
        }
    }
}
