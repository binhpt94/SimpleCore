using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.CubeTest.Tests
{
    public class CubeSpecTests
    {
        private CubeTestConfig _config;

        [SetUp]
        public void SetUp() => _config = CubeTestConfig.CreateDefault();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_config);

        // cube-edge: each cube is a cube with edge 1u (60 px on the reference screen).
        [Test]
        public void CubeEdge_IsOneUnit_AndSixtyPixelsOnReferenceScreen()
        {
            var shape = new CubeSpec(_config).Shape;

            Assert.That(shape.EdgeUnits, Is.EqualTo(1f));
            Assert.That(shape.EdgePixels, Is.EqualTo(60f));
        }

        // cube-edge: all three cubes the battle shows have that same 1u edge.
        [Test]
        public void CubeEdge_EveryOneOfTheThreeCubesIsOneUnit()
        {
            var factory = new FakeCubeFactory();
            var session = new CubeTestSession(new CubeSpec(_config), factory);

            session.Run();

            Assert.That(factory.Created.Count, Is.EqualTo(3));
            foreach (var shape in factory.Created)
            {
                Assert.That(shape.EdgeUnits, Is.EqualTo(1f));
                Assert.That(shape.EdgePixels, Is.EqualTo(60f));
            }
        }

        [Test]
        public void SessionEnd_RemovesEveryCube()
        {
            var factory = new FakeCubeFactory();
            var session = new CubeTestSession(new CubeSpec(_config), factory);
            session.Run();

            session.DisposeAsync().GetAwaiter().GetResult();

            Assert.That(factory.Removed, Is.EqualTo(3));
        }

        [Test]
        public void ConfigMissing_FailsWithItsName()
        {
            var error = Assert.Throws<ArgumentNullException>(() => new CubeSpec(null));

            Assert.That(error.ParamName, Is.EqualTo("config"));
        }

        private sealed class FakeCubeFactory : ICubeFactory
        {
            public readonly List<CubeShape> Created = new List<CubeShape>();
            public int Removed;

            public IDisposable Create(CubeShape shape)
            {
                Created.Add(shape);
                return new Removal(this);
            }

            private sealed class Removal : IDisposable
            {
                private FakeCubeFactory _owner;

                public Removal(FakeCubeFactory owner) => _owner = owner;

                public void Dispose()
                {
                    if (_owner == null) return;
                    _owner.Removed++;
                    _owner = null;
                }
            }
        }
    }
}
