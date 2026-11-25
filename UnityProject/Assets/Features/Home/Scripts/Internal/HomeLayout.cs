using System.Collections.Generic;
using UnityEngine;

namespace Game.Home
{
    internal enum HomeElementKind
    {
        /// <summary>Text only; the player cannot press it.</summary>
        Label,
        /// <summary>The one thing the player can press.</summary>
        Button,
    }

    internal enum HomeElementId
    {
        Title,
        Record,
        Play,
    }

    /// <summary>One element of the start screen: what it is and where it sits (px, top-left origin).</summary>
    internal readonly struct HomeElement
    {
        public HomeElement(HomeElementId id, HomeElementKind kind, Rect rect)
        {
            Id = id;
            Kind = kind;
            Rect = rect;
        }

        public HomeElementId Id { get; }
        public HomeElementKind Kind { get; }
        public Rect Rect { get; }
    }

    /// <summary>
    /// Everything on the start screen: the game name, the record line and one Play button, nothing else.
    /// The view builds exactly these elements.
    /// </summary>
    internal sealed class HomeLayout
    {
        private HomeLayout(Vector2 referenceScreen, IReadOnlyList<HomeElement> elements)
        {
            ReferenceScreen = referenceScreen;
            Elements = elements;
        }

        public Vector2 ReferenceScreen { get; }
        public IReadOnlyList<HomeElement> Elements { get; }

        public static HomeLayout From(HomeConfig config)
        {
            return new HomeLayout(config.ReferenceScreen, new[]
            {
                new HomeElement(HomeElementId.Title, HomeElementKind.Label, config.TitleRect),
                new HomeElement(HomeElementId.Record, HomeElementKind.Label, config.RecordRect),
                new HomeElement(HomeElementId.Play, HomeElementKind.Button, config.PlayRect),
            });
        }
    }
}
