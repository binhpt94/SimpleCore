using System;

namespace Game.Services.Popup
{
    /// <summary>Identifies one requested popup. The default value is "no popup".</summary>
    public readonly struct PopupHandle : IEquatable<PopupHandle>
    {
        public PopupHandle(int id)
        {
            Id = id;
        }

        public int Id { get; }

        public bool IsValid => Id > 0;

        public bool Equals(PopupHandle other) => Id == other.Id;

        public override bool Equals(object obj) => obj is PopupHandle other && Equals(other);

        public override int GetHashCode() => Id;
    }
}
