using System;
using Game.Core;
using Game.Run;
using Game.Services.Popup;
using UnityEngine;

namespace Game.PopupPause
{
    /// <summary>
    /// Wires the pause popup: needs the session, the run and the popup service. Install after SessionScene,
    /// Popup and Run. The prefab behind <c>PopupPause/Main</c> is listed in the scene's popup catalog.
    /// </summary>
    [CreateAssetMenu(fileName = "PopupPauseInstaller", menuName = "Game/PopupPause/Installer")]
    public sealed class PopupPauseInstaller : InstallerAsset
    {
        [SerializeField] private PopupPauseCheatState cheat = PopupPauseCheatState.None;

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            var session = Need<ISessionScene>(registry, "ISessionScene (SessionScene)");
            var run = Need<IRunFlow>(registry, "IRunFlow (Run)");
            var popups = Need<IPopupService>(registry, "IPopupService (_Services/Popup)");
            var views = Need<IPopupViews>(registry, "IPopupViews (_Services/Popup)");

            var presenter = new PausePopupPresenter(run, popups, views, new UnityPausePopupLog(), new UnityFrameClock());
            session.Join(new PopupPauseParticipant(presenter));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (cheat != PopupPauseCheatState.None) session.Join(new PausePopupCheat(run, presenter, cheat));
#endif
        }

        private static T Need<T>(IServiceRegistry registry, string name) where T : class
        {
            if (registry.TryResolve(out T service)) return service;
            throw new InvalidOperationException(
                $"PopupPauseInstaller: missing {name}. Install it before PopupPause in the Boot variant.");
        }
    }
}
