#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Game.Core;
using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>
    /// feature-popup/cheat, dev builds only. State sample-popup: the service is its own caller and opens
    /// Popup/DevSample through the normal Show and Close, to see the backdrop, the panel and the touch blocking.
    /// It never opens another feature's id and never touches the battle.
    /// </summary>
    internal static class PopupCheat
    {
        // The design names it Popup/DevSample; ids owned by a service carry its folder (validator SRC-05).
        internal static readonly PopupId DevSample = new PopupId("_Services/Popup/DevSample");

        /// <summary>Throws, naming the field, when the state needs the sample prefab and the config has none.</summary>
        internal static void Check(PopupCheatState state, PopupConfig config)
        {
            if (state == PopupCheatState.SamplePopup && config.DevSamplePrefab == null)
                throw new InvalidOperationException(
                    $"Popup config '{config.name}': devSamplePrefab is missing (needed by cheat {state}).");
        }

        /// <summary>Adds the sample popup to the scene's popups when the state needs it.</summary>
        internal static IPopupPrefabs Prefabs(PopupCheatState state, PopupConfig config, IPopupPrefabs scene) =>
            state == PopupCheatState.SamplePopup ? new DevSamplePrefabs(scene, config.DevSamplePrefab) : scene;

        /// <summary>Joins the part of the session that builds the state, after the popup service's own part.</summary>
        internal static void Join(PopupCheatState state, ISessionScene session, PopupService service)
        {
            if (state == PopupCheatState.SamplePopup) session.Join(new SamplePopupCheat(service));
        }
    }

    /// <summary>The scene's popups plus Popup/DevSample.</summary>
    internal sealed class DevSamplePrefabs : IPopupPrefabs
    {
        private readonly IPopupPrefabs scene;
        private readonly GameObject sample;

        internal DevSamplePrefabs(IPopupPrefabs scene, GameObject sample)
        {
            this.scene = scene;
            this.sample = sample ? sample : throw new ArgumentNullException(nameof(sample));
        }

        public void Prepare()
        {
            scene.Prepare();
            if (scene.TryGet(PopupCheat.DevSample, out _))
                throw new InvalidOperationException(
                    $"Popup '{PopupCheat.DevSample}' is the popup service's dev sample: remove it from the scene's popup catalog.");
        }

        public bool TryGet(PopupId id, out GameObject prefab)
        {
            if (id.Equals(PopupCheat.DevSample))
            {
                prefab = sample;
                return true;
            }
            return scene.TryGet(id, out prefab);
        }
    }

    /// <summary>State sample-popup: open the sample once the scene runs; "Đóng" closes it like any caller would.</summary>
    internal sealed class SamplePopupCheat : SessionParticipantBase
    {
        private readonly PopupService service;
        private PopupHandle handle;

        internal SamplePopupCheat(PopupService service) => this.service = service;

        protected override void OnRun()
        {
            handle = service.Show(PopupCheat.DevSample);
            if (!service.TryGetView<IDevSampleView>(handle, out var view)) return;

            view.CloseClicked += Close;
            WhenDisposed(() => view.CloseClicked -= Close);
        }

        private void Close() => service.Close(handle);   // a second click finds the handle closed and is ignored
    }
}
#endif
