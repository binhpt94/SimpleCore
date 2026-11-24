using System.Collections.Generic;
using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>
    /// Owns the popup stack (feature-popup/popup-layer/stack-rules, popup-request/request-rules): up to
    /// <c>maxOpen</c> popups, one copy per id, newest on top. Popups open only while the scene's session runs.
    /// </summary>
    internal sealed class PopupService : IPopupService, IPopupViews
    {
        private readonly IPopupPresenter presenter;
        private readonly IPopupPrefabs prefabs;
        private readonly IPopupLog log;
        private readonly int maxOpen;
        private readonly List<Entry> stack = new List<Entry>();   // bottom first
        private bool running;
        private int lastHandle;

        internal PopupService(IPopupPresenter presenter, IPopupPrefabs prefabs, IPopupLog log, int maxOpen)
        {
            this.presenter = presenter;
            this.prefabs = prefabs;
            this.log = log;
            this.maxOpen = maxOpen;
        }

        internal int OpenCount => stack.Count;

        internal PopupHandle Top => stack.Count > 0 ? stack[stack.Count - 1].Handle : default;

        /// <summary>The session is running: popups may open from now on.</summary>
        internal void Start() => running = true;

        /// <summary>The session ends: every popup goes, none reports anything more, and no popup may open again.</summary>
        internal void Stop()
        {
            running = false;
            stack.Clear();
            presenter.DismissAll();
        }

        public PopupHandle Show(PopupId id)
        {
            var prefab = prefabs.Get(id);   // throws for an unknown or default id: a typo must not pass silently

            if (!running)
            {
                log.Error($"Popup '{id}' refused: the scene is not running yet (or has ended).");
                return default;
            }
            if (IndexOf(id) >= 0)
            {
                log.Warning($"Popup '{id}' refused: it is already open.");
                return default;
            }
            if (stack.Count >= maxOpen)
            {
                log.Error($"Popup '{id}' refused: {maxOpen} popups are already open.");
                return default;
            }

            var entry = new Entry(id, new PopupHandle(++lastHandle), presenter.Present(prefab));
            stack.Add(entry);
            return entry.Handle;
        }

        public void Close(PopupHandle handle)
        {
            var index = IndexOf(handle);
            if (index < 0) return;   // closed already, never opened, or the default handle

            var entry = stack[index];
            stack.RemoveAt(index);
            presenter.Dismiss(entry.Instance);
        }

        public bool TryGetView<T>(PopupHandle handle, out T view) where T : class
        {
            var index = IndexOf(handle);
            if (index >= 0 && stack[index].Instance != null && stack[index].Instance.TryGetComponent(out view))
                return true;
            view = null;
            return false;
        }

        public bool IsOnTop(PopupHandle handle) => handle.IsValid && Top.Equals(handle);

        private int IndexOf(PopupHandle handle)
        {
            if (!handle.IsValid) return -1;
            for (var i = 0; i < stack.Count; i++)
                if (stack[i].Handle.Equals(handle)) return i;
            return -1;
        }

        private int IndexOf(PopupId id)
        {
            for (var i = 0; i < stack.Count; i++)
                if (stack[i].Id.Equals(id)) return i;
            return -1;
        }

        private sealed class Entry
        {
            internal Entry(PopupId id, PopupHandle handle, GameObject instance)
            {
                Id = id;
                Handle = handle;
                Instance = instance;
            }

            internal PopupId Id { get; }
            internal PopupHandle Handle { get; }
            internal GameObject Instance { get; }
        }
    }
}
