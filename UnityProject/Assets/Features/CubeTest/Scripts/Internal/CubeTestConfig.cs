using UnityEngine;

namespace Game.CubeTest
{
    /// <summary>
    /// Numbers of the test cubes (live: feature-cube-test/cube-cases/cube-spec, Params table).
    /// The asset lives in FeatureAssets/Private; defaults match the design so tests can build one in memory.
    /// </summary>
    internal sealed class CubeTestConfig : ScriptableObject
    {
        [Tooltip("Cube edge, in world units (1u = 60 px on the 1920x1080 reference screen).")]
        [SerializeField] private float edgeUnits = 1f;

        [Tooltip("Pixels per world unit on the reference screen (feature-battlefield layer-values).")]
        [SerializeField] private float pixelsPerUnit = 60f;

        public float EdgeUnits => edgeUnits;
        public float PixelsPerUnit => pixelsPerUnit;

        internal static CubeTestConfig CreateDefault() => CreateInstance<CubeTestConfig>();
    }
}
