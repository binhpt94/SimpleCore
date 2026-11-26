using System;

namespace Game.CubeTest
{
    /// <summary>Shape of one test cube: a cube with equal edges, measured in world units and reference-screen pixels.</summary>
    internal readonly struct CubeShape
    {
        public CubeShape(float edgeUnits, float pixelsPerUnit)
        {
            EdgeUnits = edgeUnits;
            EdgePixels = edgeUnits * pixelsPerUnit;
        }

        /// <summary>Edge length along x, y and z (same on every axis), in world units.</summary>
        public float EdgeUnits { get; }

        /// <summary>Edge length on the 1920x1080 reference screen, in pixels.</summary>
        public float EdgePixels { get; }
    }

    /// <summary>Game rules of the test cubes, read from the config. No Unity objects here.</summary>
    internal sealed class CubeSpec
    {
        private readonly CubeTestConfig _config;

        public CubeSpec(CubeTestConfig config)
        {
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            if (config.EdgeUnits <= 0f) throw new ArgumentException("CubeTestConfig: edge must be greater than 0.");
            if (config.PixelsPerUnit <= 0f) throw new ArgumentException("CubeTestConfig: pixels per unit must be greater than 0.");
        }

        public CubeShape Shape => new CubeShape(_config.EdgeUnits, _config.PixelsPerUnit);
    }
}
