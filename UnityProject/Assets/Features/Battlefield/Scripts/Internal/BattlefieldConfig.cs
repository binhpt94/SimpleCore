using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battlefield
{
    /// <summary>Kind of one decor piece. Grass and stone are flat on the path; flag and fence stand in the far scenery.</summary>
    internal enum DecorKind
    {
        Grass,
        Stone,
        Flag,
        Fence,
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
    /// One strip of far scenery, standing on the far base line. <see cref="fade"/> mixes the base colour towards the
    /// sky colour (0 = base colour, 1 = sky colour); <see cref="artScale"/> is the size of the hills drawn in it,
    /// relative to the front strip.
    /// </summary>
    [Serializable]
    internal struct FarBand
    {
        public string id;
        public float top;
        public float artScale;
        public Color baseColor;
        [Range(0f, 1f)] public float fade;
        public Sprite sprite;

        public FarBand(string id, float top, float artScale, Color baseColor, float fade, Sprite sprite = null)
        {
            this.id = id;
            this.top = top;
            this.artScale = artScale;
            this.baseColor = baseColor;
            this.fade = fade;
            this.sprite = sprite;
        }
    }

    /// <summary>
    /// Every number of the battle backdrop (node layer-values, table LayerParams, column of the phase in work).
    /// The asset in FeatureAssets/Private holds the values; the code only reads them. Units are world units (u) of
    /// the Game scene's camera, ground line at y = 0.
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
        [SerializeField] private float groundBottom = -4f;

        [Header("Far scenery, back to front (u)")]
        [SerializeField] private float farBase;
        [SerializeField] private List<FarBand> farBands = new List<FarBand>();

        [Header("Decor (u)")]
        [SerializeField] private Vector2 grassSize = new Vector2(0.8f, 0.3f);
        [SerializeField] private Vector2 stoneSize = new Vector2(0.6f, 0.3f);
        [SerializeField] private Vector2 flagSize = new Vector2(0.5f, 1.2f);
        [SerializeField] private Vector2 fenceSize = new Vector2(1.4f, 0.6f);
        [SerializeField] private float standDecorBase;
        [Range(0f, 1f)] [SerializeField] private float standDecorFade = 0.2f;
        [SerializeField] private float castleClearHalfWidth = 3f;
        [SerializeField] private List<DecorSlot> decor = new List<DecorSlot>();
        [SerializeField] private bool decorSway;
        [SerializeField] private bool backdropByWave;

        [Header("HUD edges that must only cover sky and ground (u)")]
        [SerializeField] private float hudTopEdge = 11.7f;
        [SerializeField] private float hudBottomEdge = -1.3f;

        [Header("Draw order, back to front (all below castle, units, effects); far strips take farOrder + index")]
        [SerializeField] private int skyOrder = -500;
        [SerializeField] private int farOrder = -450;
        [SerializeField] private int standDecorOrder = -300;
        [SerializeField] private int groundOrder = -200;
        [SerializeField] private int decorOrder = -100;
        [SerializeField] private int lowestBattleOrder;

        [Header("Sprites (empty: a flat colour box is drawn instead)")]
        [SerializeField] private Sprite skySprite;
        [SerializeField] private Sprite groundSprite;
        [SerializeField] private Sprite grassSprite;
        [SerializeField] private Sprite stoneSprite;
        [SerializeField] private Sprite flagSprite;
        [SerializeField] private Sprite fenceSprite;

        [Header("Colours (shared palette of the root style); boxes use them when a sprite is empty")]
        [SerializeField] private Color skyColor = new Color(0.486f, 0.816f, 1f);
        [SerializeField] private Color groundColor = new Color(0.49f, 0.36f, 0.24f);
        [SerializeField] private Color grassColor = new Color(0.36f, 0.62f, 0.27f);
        [SerializeField] private Color stoneColor = new Color(0.55f, 0.55f, 0.58f);
        [SerializeField] private Color flagColor = new Color(0.949f, 0.31f, 0.627f);
        [SerializeField] private Color flagPoleColor = new Color(1f, 0.776f, 0.122f);
        [SerializeField] private Color fenceColor = new Color(1f, 0.945f, 0.894f);
        [SerializeField] private Color fenceShadeColor = new Color(1f, 0.776f, 0.122f);

        internal float PixelsPerUnit { get => pixelsPerUnit; set => pixelsPerUnit = value; }
        internal float ReferenceWidthPx { get => referenceWidthPx; set => referenceWidthPx = value; }
        internal float ReferenceHeightPx { get => referenceHeightPx; set => referenceHeightPx = value; }
        internal float GroundLinePx { get => groundLinePx; set => groundLinePx = value; }

        internal float CoverLeft { get => coverLeft; set => coverLeft = value; }
        internal float CoverRight { get => coverRight; set => coverRight = value; }
        internal float SkyTop { get => skyTop; set => skyTop = value; }
        internal float GroundBottom { get => groundBottom; set => groundBottom = value; }

        internal float FarBase { get => farBase; set => farBase = value; }
        internal List<FarBand> FarBands { get => farBands; set => farBands = value; }

        internal Vector2 GrassSize { get => grassSize; set => grassSize = value; }
        internal Vector2 StoneSize { get => stoneSize; set => stoneSize = value; }
        internal Vector2 FlagSize { get => flagSize; set => flagSize = value; }
        internal Vector2 FenceSize { get => fenceSize; set => fenceSize = value; }
        internal float StandDecorBase { get => standDecorBase; set => standDecorBase = value; }
        internal float StandDecorFade { get => standDecorFade; set => standDecorFade = value; }
        internal float CastleClearHalfWidth { get => castleClearHalfWidth; set => castleClearHalfWidth = value; }
        internal List<DecorSlot> Decor { get => decor; set => decor = value; }
        internal bool DecorSway { get => decorSway; set => decorSway = value; }
        internal bool BackdropByWave { get => backdropByWave; set => backdropByWave = value; }

        internal float HudTopEdge { get => hudTopEdge; set => hudTopEdge = value; }
        internal float HudBottomEdge { get => hudBottomEdge; set => hudBottomEdge = value; }

        internal int SkyOrder { get => skyOrder; set => skyOrder = value; }
        internal int FarOrder { get => farOrder; set => farOrder = value; }
        internal int StandDecorOrder { get => standDecorOrder; set => standDecorOrder = value; }
        internal int GroundOrder { get => groundOrder; set => groundOrder = value; }
        internal int DecorOrder { get => decorOrder; set => decorOrder = value; }
        internal int LowestBattleOrder { get => lowestBattleOrder; set => lowestBattleOrder = value; }

        internal Sprite SkySprite { get => skySprite; set => skySprite = value; }
        internal Sprite GroundSprite { get => groundSprite; set => groundSprite = value; }
        internal Sprite GrassSprite { get => grassSprite; set => grassSprite = value; }
        internal Sprite StoneSprite { get => stoneSprite; set => stoneSprite = value; }
        internal Sprite FlagSprite { get => flagSprite; set => flagSprite = value; }
        internal Sprite FenceSprite { get => fenceSprite; set => fenceSprite = value; }

        internal Color SkyColor => skyColor;
        internal Color GroundColor => groundColor;
        internal Color FlagPoleColor => flagPoleColor;
        internal Color FenceShadeColor => fenceShadeColor;

        internal static bool IsStanding(DecorKind kind) => kind == DecorKind.Flag || kind == DecorKind.Fence;

        internal Vector2 SizeOf(DecorKind kind)
        {
            switch (kind)
            {
                case DecorKind.Grass: return grassSize;
                case DecorKind.Stone: return stoneSize;
                case DecorKind.Flag: return flagSize;
                default: return fenceSize;
            }
        }

        internal Sprite SpriteOf(DecorKind kind)
        {
            switch (kind)
            {
                case DecorKind.Grass: return grassSprite;
                case DecorKind.Stone: return stoneSprite;
                case DecorKind.Flag: return flagSprite;
                default: return fenceSprite;
            }
        }

        internal Color ColorOf(DecorKind kind)
        {
            switch (kind)
            {
                case DecorKind.Grass: return grassColor;
                case DecorKind.Stone: return stoneColor;
                case DecorKind.Flag: return flagColor;
                default: return fenceColor;
            }
        }

        /// <summary>How far the colour of a decor piece is mixed towards the sky: standing decor reads as far away.</summary>
        internal float FadeOf(DecorKind kind) => IsStanding(kind) ? standDecorFade : 0f;
    }
}
