using System;
using Game.Core;
using Game.Run;
using Game.Services.Popup;
using UnityEngine;

namespace Game.PopupLose
{
    /// <summary>
    /// Wires the lose popup into the scene session. Install after Popup and Run.
    /// The popup prefab itself is listed under id PopupLose/Main in the scene's popup catalog (_Composition).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/PopupLose/Installer", fileName = "PopupLoseInstaller")]
    public sealed class PopupLoseInstaller : InstallerAsset
    {
        [Tooltip("Dev builds only: build this cheat state as soon as the session runs (node feature-popup-lose/cheat).")]
        [SerializeField] private LoseCheatState _cheat = LoseCheatState.None;

        internal LoseCheatState Cheat
        {
            get => _cheat;
            set => _cheat = value;
        }

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            var session = Need<ISessionScene>(registry, "SessionScene");
            var flow = Need<IRunFlow>(registry, "Run");
            var score = Need<IRunScore>(registry, "Run");
            var popups = Need<IPopupService>(registry, "Popup");
            var views = Need<IPopupViews>(registry, "Popup");
            var revive = Need<IRunRevive>(registry, "Run");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_cheat != LoseCheatState.None)
            {
                var cheatPopup = new LosePopupParticipant(flow, revive, FixedRunScore.For(_cheat), popups, views, new UnityLoseLog());
                session.Join(cheatPopup);
                session.Join(new LoseCheatParticipant(cheatPopup));
                return;
            }
#endif
            session.Join(new LosePopupParticipant(flow, revive, score, popups, views, new UnityLoseLog()));
        }

        private static T Need<T>(IServiceRegistry registry, string owner) where T : class
        {
            if (registry.TryResolve<T>(out var service)) return service;
            throw new InvalidOperationException(
                $"PopupLose: {typeof(T).Name} is missing. Install {owner} before PopupLose.");
        }
    }
}
