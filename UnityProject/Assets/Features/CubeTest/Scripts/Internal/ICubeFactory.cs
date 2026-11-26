using System;

namespace Game.CubeTest
{
    /// <summary>
    /// Builds one test cube in the scene. The real one (View) makes an engine cube primitive;
    /// tests replace it with a fake. Disposing the result removes the cube.
    /// </summary>
    internal interface ICubeFactory
    {
        IDisposable Create(CubeShape shape);
    }
}
