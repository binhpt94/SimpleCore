using System;
using Game.Core;

namespace Game.CubeTest
{
    /// <summary>
    /// Life of the test cubes inside the battle scene: builds the three cubes when the session runs,
    /// removes them when it ends. Sends nothing to and reads nothing from other features.
    /// </summary>
    internal sealed class CubeTestSession : SessionParticipantBase
    {
        public const int CubeCount = 3;

        private readonly CubeSpec _spec;
        private readonly ICubeFactory _factory;

        public CubeTestSession(CubeSpec spec, ICubeFactory factory)
        {
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        protected override void OnRun()
        {
            var shape = _spec.Shape;
            for (var i = 0; i < CubeCount; i++)
            {
                var cube = _factory.Create(shape);
                WhenDisposed(cube.Dispose);
            }
        }
    }
}
