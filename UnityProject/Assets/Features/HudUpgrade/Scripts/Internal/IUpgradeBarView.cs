using System;
using System.Collections.Generic;

namespace Game.HudUpgrade
{
    /// <summary>What a button's picture stands for.</summary>
    internal enum ButtonPictureKind
    {
        Hero,
        CastleHealth,
        CastleAttack,
        CastleSkill,
    }

    /// <summary>The fixed part of one button: its picture and which group of the bar it sits in.</summary>
    internal readonly struct ButtonPicture : IEquatable<ButtonPicture>
    {
        public ButtonPicture(ButtonPictureKind kind, string heroCode, string caption, int group)
        {
            Kind = kind;
            HeroCode = heroCode;
            Caption = caption;
            Group = group;
        }

        public ButtonPictureKind Kind { get; }

        /// <summary>Hero code for a hero button, to find its portrait; null otherwise.</summary>
        public string HeroCode { get; }

        /// <summary>Hero name or castle item name; shown when the picture has no art.</summary>
        public string Caption { get; }

        /// <summary>0 for the hero group, 1 for the castle group. A new group starts after a wider gap.</summary>
        public int Group { get; }

        public bool Equals(ButtonPicture other) =>
            Kind == other.Kind && HeroCode == other.HeroCode && Caption == other.Caption && Group == other.Group;

        public override bool Equals(object obj) => obj is ButtonPicture other && Equals(other);

        public override int GetHashCode() => ((int)Kind * 397) ^ (HeroCode?.GetHashCode() ?? 0);
    }

    /// <summary>The changing part of one button, ready to show.</summary>
    internal readonly struct ButtonLook
    {
        public ButtonLook(string levelText, string priceText, bool lit)
        {
            LevelText = levelText;
            PriceText = priceText;
            Lit = lit;
        }

        /// <summary>"Cấp N".</summary>
        public string LevelText { get; }

        /// <summary>Price of the next level, already shortened.</summary>
        public string PriceText { get; }

        /// <summary>Lit when buying now would succeed; dim otherwise.</summary>
        public bool Lit { get; }
    }

    /// <summary>The upgrade bar on screen. Shows what it is told and reports taps; decides nothing.</summary>
    internal interface IUpgradeBarView
    {
        /// <summary>Replaces every button with one per entry, in order; a tap on button i calls <paramref name="onTap"/>(i).</summary>
        void Build(IReadOnlyList<ButtonPicture> buttons, Action<int> onTap);

        void SetButton(int index, ButtonLook look);

        /// <summary>Plays the "bought" bounce once on button <paramref name="index"/>.</summary>
        void Bounce(int index);

        /// <summary>Flashes the level number of button <paramref name="index"/> once, right after a level was bought.</summary>
        void FlashLevel(int index);

        /// <summary>Plays the short "cannot buy" shake once on button <paramref name="index"/>.</summary>
        void Shake(int index);

        /// <summary>Shows or clears the "hero is down" mark on hero button <paramref name="index"/>.</summary>
        void SetDown(int index, bool down);

        void SetVisible(bool visible);
    }

    /// <summary>Makes and removes the bar view, so the session code runs in tests without Unity objects.</summary>
    internal interface IUpgradeBarViewFactory
    {
        IUpgradeBarView Create();

        void Destroy(IUpgradeBarView view);
    }
}
