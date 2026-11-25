using System.Collections.Generic;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// A level number drawn above a hero with seven-segment digits made of plain box sprites, so it needs no font
    /// package. Shows only what the hero view hands it; centred on its root, which the view places.
    /// </summary>
    internal sealed class HeroLevelLabel
    {
        // Segments a (top), b (upper right), c (lower right), d (bottom), e (lower left), f (upper left), g (middle).
        private static readonly bool[][] Digits =
        {
            new[] { true, true, true, true, true, true, false },     // 0
            new[] { false, true, true, false, false, false, false }, // 1
            new[] { true, true, false, true, true, false, true },    // 2
            new[] { true, true, true, true, false, false, true },    // 3
            new[] { false, true, true, false, false, true, true },   // 4
            new[] { true, false, true, true, false, true, true },    // 5
            new[] { true, false, true, true, true, true, true },     // 6
            new[] { true, true, true, false, false, false, false },  // 7
            new[] { true, true, true, true, true, true, true },      // 8
            new[] { true, true, true, true, false, true, true },     // 9
        };

        private readonly Sprite _box;
        private readonly Color _color;
        private readonly int _sortingOrder;
        private readonly List<SpriteRenderer[]> _digits = new List<SpriteRenderer[]>();

        public HeroLevelLabel(Transform root, Sprite box, Color color, int sortingOrder)
        {
            Root = root;
            _box = box;
            _color = color;
            _sortingOrder = sortingOrder;
        }

        public Transform Root { get; }

        public void Show(int level, float digitHeight)
        {
            var text = Mathf.Max(0, level).ToString();
            while (_digits.Count < text.Length) _digits.Add(MakeDigit(_digits.Count));

            var height = digitHeight;
            var width = height * 0.55f;
            var thick = height * 0.14f;
            var step = width + thick * 1.5f;
            var left = -(text.Length - 1) * step * 0.5f;

            for (var i = 0; i < _digits.Count; i++)
            {
                var segments = _digits[i];
                var used = i < text.Length;
                var lit = used ? Digits[text[i] - '0'] : null;
                var cx = left + i * step;
                for (var s = 0; s < 7; s++)
                {
                    var renderer = segments[s];
                    renderer.enabled = used && lit[s];
                    if (!renderer.enabled) continue;
                    Place(renderer.transform, s, cx, width, height, thick);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
        }

        private static void Place(Transform t, int segment, float cx, float width, float height, float thick)
        {
            var half = height * 0.5f;
            var sideX = width * 0.5f;
            var vertical = half - thick * 0.5f;
            switch (segment)
            {
                case 0: Set(t, cx, height - thick * 0.5f, width, thick); break;
                case 1: Set(t, cx + sideX, half + vertical * 0.5f + thick * 0.25f, thick, vertical); break;
                case 2: Set(t, cx + sideX, vertical * 0.5f + thick * 0.25f, thick, vertical); break;
                case 3: Set(t, cx, thick * 0.5f, width, thick); break;
                case 4: Set(t, cx - sideX, vertical * 0.5f + thick * 0.25f, thick, vertical); break;
                case 5: Set(t, cx - sideX, half + vertical * 0.5f + thick * 0.25f, thick, vertical); break;
                default: Set(t, cx, half, width, thick); break;
            }
        }

        private static void Set(Transform t, float x, float y, float w, float h)
        {
            t.localPosition = new Vector3(x, y, 0f);
            t.localScale = new Vector3(w, h, 1f);
        }

        private SpriteRenderer[] MakeDigit(int index)
        {
            var segments = new SpriteRenderer[7];
            for (var s = 0; s < 7; s++)
            {
                var child = new GameObject($"Digit {index} segment {s}");
                child.transform.SetParent(Root, false);
                var renderer = child.AddComponent<SpriteRenderer>();
                renderer.sprite = _box;
                renderer.color = _color;
                renderer.sortingOrder = _sortingOrder;
                segments[s] = renderer;
            }
            return segments;
        }
    }
}
