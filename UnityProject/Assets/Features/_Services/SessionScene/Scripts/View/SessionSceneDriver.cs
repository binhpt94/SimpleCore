using UnityEngine;

namespace Game.Services.SessionScene
{
    /// <summary>
    /// Passes two Unity moments to the session and decides nothing itself. Start comes after every
    /// Awake of the scene, so after Boot ran all installers; OnDestroy comes when the scene goes away.
    /// </summary>
    internal sealed class SessionSceneDriver : MonoBehaviour, ISessionDriver
    {
        private ISessionLifecycle session;

        void ISessionDriver.Drive(ISessionLifecycle session)
        {
            this.session = session;
        }

        private void Start()
        {
            if (session == null)
            {
                Debug.LogError("SessionSceneDriver has no session: it must be created by SessionSceneInstaller, " +
                               "not placed in a scene by hand.", this);
                return;
            }

            session.Begin();
        }

        private void OnDestroy()
        {
            // Unity does not wait here: only the synchronous part of each DisposeAsync is sure to run.
            session?.RequestEnd();
        }
    }
}
