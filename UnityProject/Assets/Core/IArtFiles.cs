using Object = UnityEngine.Object;

namespace Game.Core
{
    /// <summary>
    /// Where <see cref="Art"/> reads its files: a path under Resources without extension
    /// (<c>Art/F/id/Simple</c>, <c>Art/F/id/Detail</c>, <c>ArtChoice</c>). Tests give a fake.
    /// </summary>
    internal interface IArtFiles
    {
        /// <summary>The file at that path, or null when there is none.</summary>
        Object Load(string path);
    }
}
