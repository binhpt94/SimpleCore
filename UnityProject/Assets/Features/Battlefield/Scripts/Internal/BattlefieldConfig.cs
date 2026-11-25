using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battlefield
{
    /// <summary>Kind of one flat decor piece on the ground.</summary>
    internal enum DecorKind
    {
        Grass,
        Stone,
    }

    /// <summary>One decor piece: what it is and where its centre stands on the ground line (x, in u).</summary>
    [Serializable]
    internal struct DecorSlot
    {
        public DecorKind kind;
        public float centerX;

        public DecorSlot(DecorKind kind, float centerX)
        {
            this.kind = kind;
            this.centerX = centerX;
        }
    }

    /// <summary>
    /// Every number of the battle backdrop (node layer-values). The asset in FeatureAssets/Private holds the values;
    /// the code only reads them. Units are world units (u) of the Game scene's camera, ground line at y = 0.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Battlefield/Config", fileName = "BattlefieldConfig")]
    internal sealed class BattlefieldConfig : ScriptableObject
    {
        [Header("Camera of the Game scene (reference screen)")]
        [SerializeField] private float pixelsPerUnit = 60f;
        [SerializeField] private float referenceWidthPx = 1920f;
        [SerializeField] private float referenceHeightPx = 1080f;
        [SerializeField] private float groundLinePx = 840f;

        [Header("Layers (u)")]
        [SerializeField] private float coverLeft = -21f;
        [SerializeField] private float coverRight = 21f;
        [SerializeField] private float skyTop = 14f;
        [SerializeField] private float farHeight = 2.5f;
        [SerializeField] private float groundBottom = -4f;

        [Header("Decor (u)")]
        [SerializeField] private Vector2 grassSize = new Vector2(0.8f, 0.3f);
        [SerializeField] private Vector2 stoneSize = new Vector2(0.6f, 0.3f);
        [SerializeField] private float castleClearHalfWidth = 3f;
        [SerializeField] private List<DecorSlot> decor = new List<DecorSlot>();

        [Header("HUD edges that must only cover sky and ground (u)")]
        [SerializeField] private float hudTopEdge = 11.7f;
        [SerializeField] private float hudBottomEdge = -1.3f;

        [Header("Draw order (all below castle, units, effects)")]
        [SerializeField] private int skyOrder = -400;
        [SerializeField] private int farOrder = -300;
        [SerializeField] private int groundOrder = -200;
        [SerializeField] private int decorOrder = -100;
        [SerializeField] private int lowestBattleOrder = 0;

        [Header("Sprites (empty: a flat colour box is drawn instead)")]
        [SerializeField] private Sprite skySprite;
        [SerializeField] private Sprite farSprite;
        [SerializeField] private Sprite groundSprite;
        [SerializeField] private Sprite grassSprite;
        [SerializeField] private Sprite stoneSprite;

        [Header("Colours used when a sprite is empty")]
        [SerializeField] private Color skyColor = new Color(0.62f, 0.80f, 0.93f);
        [SerializeField] private Color farColor = new Color(0.33f, 0.52f, 0.40f);
        [SerializeField] private Color groundColor = new Color(0.49f, 0.36f, 0.24f);
        [SerializeField] private Color grassColor = new Color(0.36f, 0.62f, 0.27f);
        [SerializeField] private Color stoneColor = new Color(0.55f, 0.55f, 0.58f);

        internal float PixelsPerUnit { get => pixelsPerUnit; set => pixelsPerUnit = value; }
        internal float ReferenceWidthPx { get => referenceWidthPx; set => referenceWidthPx = value; }
        internal float ReferenceHeightPx { get => referenceHeightPx; set => referenceHeightPx = value; }
        internal float GroundLinePx { get => groundLinePx; set => groundLinePx = value; }

        internal float CoverLeft { get => coverLeft; set => coverLeft = value; }
        internal float CoverRight { get => coverRight; set => coverRight = value; }
        internal float SkyTop { get => skyTop; set => skyTop = value; }
        internal float FarHeight { get => farHeight; set => farHeight = value; }
        internal float GroundBottom { get => groundBottom; set => groundBottom = value; }

        internal Vector2 GrassSize { get => grassSize; set => grassSize = value; }
        internal Vector2 StoneSize { get => stoneSize; set => stoneSize = value; }
        internal float CastleClearHalfWidth { get => castleClearHalfWidth; set => castleClearHalfWidth = value; }
        internal List<DecorSlot> Decor { get => decor; set => decor = value; }

        internal float HudTopEdge { get => hudTopEdge; set => hudTopEdge = value; }
        internal float HudBottomEdge { get => hudBottomEdge; set => hudBottomEdge = value; }

        internal int SkyOrder { get => skyOrder; set => skyOrder = value; }
        internal int FarOrder { get => farOrder; set => farOrder = value; }
        internal int GroundOrder { get => groundOrder; set => groundOrder = value; }
        internal int DecorOrder { get => decorOrder; set => decorOrder = value; }
        internal int LowestBattleOrder { get => lowestBattleOrder; set => lowestBattleOrder = value; }

        internal Sprite SkySprite { get => skySprite; set => skySprite = value; }
        internal Sprite FarSprite { get => farSprite; set => farSprite = value; }
        internal Sprite GroundSprite { get => groundSprite; set => groundSprite = value; }
        internal Sprite GrassSprite { get => grassSprite; set => grassSprite = value; }
        internal Sprite StoneSprite { get => stoneSprite; set => stoneSprite = value; }

        internal Color SkyColor => skyColor;
        internal Color FarColor => farColor;
        internal Color GroundColor => groundColor;
        internal Color GrassColor => grassColor;
        internal Color StoneColor => stoneColor;

        internal Vector2 SizeOf(DecorKind kind) => kind == DecorKind.Grass ? grassSize : stoneSize;
        internal Sprite SpriteOf(DecorKind kind) => kind == DecorKind.Grass ? grassSprite : stoneSprite;
        internal Color ColorOf(DecorKind kind) => kind == DecorKind.Grass ? grassColor : stoneColor;
    }
}
