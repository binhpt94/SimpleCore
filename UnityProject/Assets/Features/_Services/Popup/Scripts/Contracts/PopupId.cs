using System;
using System.Text.RegularExpressions;

namespace Game.Services.Popup
{
    /// <summary>
    /// Names one popup: "&lt;Owner&gt;/&lt;Name&gt;", Owner being the feature that owns the prefab,
    /// for example "Shop/Purchased". The owner declares each id once, as a static readonly field.
    /// The prefab behind an id comes from the scene's popup catalog in _Composition.
    /// </summary>
    public readonly struct PopupId : IEquatable<PopupId>
    {
        private static readonly Regex Format =
            new Regex(@"^(_Services/)?[A-Z][A-Za-z0-9]*/[A-Z][A-Za-z0-9]*(\.[A-Z][A-Za-z0-9]*)*$");

        /// <summary>Throws <see cref="ArgumentException"/> when <paramref name="value"/> is not "&lt;Owner&gt;/&lt;Name&gt;".</summary>
        public PopupId(string value)
        {
            if (!IsValid(value))
                throw new ArgumentException(
                    $"'{value}' is not a popup id: use <Owner>/<Name>, for example Shop/Purchased.", nameof(value));
            Value = value;
        }

        /// <summary>The id text; null for the default value, which names no popup.</summary>
        public string Value { get; }

        public static bool IsValid(string value) => value != null && Format.IsMatch(value);

        public bool Equals(PopupId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is PopupId other && Equals(other);

        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;

        public override string ToString() => Value ?? "(no popup)";
    }
}
