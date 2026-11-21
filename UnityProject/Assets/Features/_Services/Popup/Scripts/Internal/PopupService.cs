using System.Collections.Generic;
using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>Owns the popup queue: one popup open at a time, the rest wait in request order.</summary>
    internal sealed class PopupService : IPopupService
    {
        private readonly IPopupPresenter presenter;
        private readonly IPopupPrefabs prefabs;
        private readonly List<Entry> waiting = new List<Entry>();
        private Entry open;
        private int lastId;

        internal PopupService(IPopupPresenter presenter, IPopupPrefabs prefabs)
        {
            this.presenter = presenter;
            this.prefabs = prefabs;
        }

        internal PopupHandle Current => open != null ? open.Handle : default;

        internal int WaitingCount => waiting.Count;

        public PopupHandle Show(PopupId id)
        {
            var prefab = prefabs.Get(id);   // throws for an unknown or default id: a typo must not pass silently

            var entry = new Entry(new PopupHandle(++lastId), prefab);
            if (open == null) Open(entry);
            else waiting.Add(entry);
            return entry.Handle;
        }

        public void Close(PopupHandle handle)
        {
            if (!handle.IsValid) return;

            if (open != null && open.Handle.Equals(handle))
            {
                presenter.Dismiss(open.Instance);
                open = null;
                if (waiting.Count > 0)
                {
                    var next = waiting[0];
                    waiting.RemoveAt(0);
                    Open(next);
                }
                return;
            }

            waiting.RemoveAll(entry => entry.Handle.Equals(handle));
        }

        private void Open(Entry entry)
        {
            entry.Instance = presenter.Present(entry.Prefab);
            open = entry;
        }

        private sealed class Entry
        {
            internal Entry(PopupHandle handle, GameObject prefab)
            {
                Handle = handle;
                Prefab = prefab;
            }

            internal PopupHandle Handle { get; }
            internal GameObject Prefab { get; }
            internal GameObject Instance { get; set; }
        }
    }
}
