using System;
using System.Collections.Generic;

namespace Game.TeamSelect
{
    /// <summary>
    /// The slots of a team (design: team-slots/slots-spec). Each slot holds one hero code or is empty;
    /// a hero is in at most one slot. Taps are handled one after another in the order they arrive.
    /// </summary>
    internal sealed class TeamSlots
    {
        private readonly string[] _slots;

        public TeamSlots(int slotCount)
        {
            if (slotCount <= 0) throw new ArgumentOutOfRangeException(nameof(slotCount));
            _slots = new string[slotCount];
        }

        public int Count => _slots.Length;

        /// <summary>Raised after any slot changed.</summary>
        public event Action Changed;

        /// <summary>Hero code in slot <paramref name="index"/> (0 = slot 1), or null when empty.</summary>
        public string this[int index] => _slots[index];

        public bool IsFull
        {
            get
            {
                foreach (var code in _slots)
                    if (code == null) return false;
                return true;
            }
        }

        public bool Contains(string heroCode) => IndexOf(heroCode) >= 0;

        public int IndexOf(string heroCode)
        {
            if (heroCode == null) return -1;
            for (var i = 0; i < _slots.Length; i++)
                if (_slots[i] == heroCode) return i;
            return -1;
        }

        /// <summary>
        /// A hero card was tapped: a picked hero leaves its slot; an unpicked one goes into the empty slot
        /// with the smallest number; when the team is full nothing changes. Returns true when a slot changed.
        /// </summary>
        public bool TapHero(string heroCode)
        {
            if (heroCode == null) return false;
            var at = IndexOf(heroCode);
            if (at >= 0)
            {
                _slots[at] = null;
                Changed?.Invoke();
                return true;
            }
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null) continue;
                _slots[i] = heroCode;
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>A slot was tapped: its hero leaves the team; an empty slot does nothing. Others keep their slot.</summary>
        public bool TapSlot(int index)
        {
            if (index < 0 || index >= _slots.Length || _slots[index] == null) return false;
            _slots[index] = null;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Fills the slots from <paramref name="team"/> (slot order kept). A code that is not in
        /// <paramref name="isAvailable"/>, repeated, or past the last slot leaves that slot empty.
        /// </summary>
        public void Fill(IReadOnlyList<string> team, Func<string, bool> isAvailable)
        {
            for (var i = 0; i < _slots.Length; i++) _slots[i] = null;
            if (team != null)
            {
                for (var i = 0; i < _slots.Length && i < team.Count; i++)
                {
                    var code = team[i];
                    if (code == null || Contains(code)) continue;
                    if (isAvailable != null && !isAvailable(code)) continue;
                    _slots[i] = code;
                }
            }
            Changed?.Invoke();
        }

        /// <summary>The hero codes in slot order; only meaningful when <see cref="IsFull"/>.</summary>
        public string[] ToArray()
        {
            var copy = new string[_slots.Length];
            Array.Copy(_slots, copy, _slots.Length);
            return copy;
        }
    }
}
