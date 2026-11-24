using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;

namespace Game.Services.Popup
{
    /// <summary>
    /// The popup service's part in the scene's session: checks every popup of the scene while loading,
    /// lets popups open once running, and removes them all when the session ends.
    /// </summary>
    internal sealed class PopupParticipant : SessionParticipantBase
    {
        private readonly PopupService service;
        private readonly IPopupPrefabs prefabs;

        internal PopupParticipant(PopupService service, IPopupPrefabs prefabs)
        {
            this.service = service;
            this.prefabs = prefabs;
        }

        protected override UniTask OnLoadAsync(CancellationToken cancellation)
        {
            prefabs.Prepare();   // throws, naming the id: the load step fails and the scene does not run
            return UniTask.CompletedTask;
        }

        protected override void OnRun()
        {
            service.Start();
        }

        protected override UniTask OnDisposeAsync()
        {
            service.Stop();
            return UniTask.CompletedTask;
        }
    }
}
