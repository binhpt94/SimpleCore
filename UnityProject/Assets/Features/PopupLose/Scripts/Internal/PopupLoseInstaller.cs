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
    /// Dev builds: the cheat list half of this class is in Cheat/PopupLoseInstaller.Cheat.cs.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/PopupLose/Installer", fileName = "PopupLoseInstaller")]
    public sealed partial class PopupLoseInstaller : InstallerAsset
    {
        [Tooltip("Dev builds only: build this cheat state as soon as the session runs (node feature-popup-lose/cheat). " +
                 "Ignored once a state or normal play was picked in this run of the game.")]
        [SerializeField] private LoseCheatState _cheat = LoseCheatState.None;

        [Tooltip("Shows \"Về màn bắt đầu\". Off at IDEA (no start screen yet); on from PROTOTYPE (node actions-spec, Thông số theo giai đoạn).")]
        [SerializeField] private bool _homeButton = true;

        internal LoseCheatState Cheat
        {
            get => _cheat;
            set => _cheat = value;
        }

        internal bool HomeButton
        {
            get => _homeButton;
            set => _homeButton = value;
        }

        public override void Install(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var cheats = NeedCheatSelection(registry);
#endif
            var session = Need<ISessionScene>(registry, "SessionScene");
            var flow = Need<IRunFlow>(registry, "Run");
            var score = Need<IRunScore>(registry, "Run");
            var popups = Need<IPopupService>(registry, "Popup");
            var views = Need<IPopupViews>(registry, "Popup");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var state = CheatToBuild(cheats);
            if (state != LoseCheatState.None)
            {
                cheats.ReportRunning(LoseCheats.KeyOf(state));
                var cheatPopup = new LosePopupParticipant(flow, FixedRunScore.For(state), popups, views, new UnityLoseLog(), _homeButton);
                session.Join(cheatPopup);
                session.Join(new LoseCheatParticipant(cheatPopup));
                return;
            }
#endif
            session.Join(new LosePopupParticipant(flow, score, popups, views, new UnityLoseLog(), _homeButton));
        }

        private static T Need<T>(IServiceRegistry registry, string owner) where T : class
        {
            if (registry.TryResolve<T>(out var service)) return service;
            throw new InvalidOperationException(
                $"PopupLose: {typeof(T).Name} is missing. Install {owner} before PopupLose.");
        }
    }
}
