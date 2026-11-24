using Cysharp.Threading.Tasks;
using Game.Services.SessionScene;

namespace Game.SceneFlow
{
    /// <summary>
    /// How the Game scene tells the Loading scene how far it has loaded, and that it can be played or
    /// never will be. The two scenes have separate registries, so this travels on a scene object
    /// (ISceneGateCarrier). It remembers its answer: whoever asks late still gets it.
    /// </summary>
    internal sealed class SceneGate
    {
        private readonly UniTaskCompletionSource<bool> answer = new UniTaskCompletionSource<bool>();

        internal SceneGate(ISessionLoadProgress progress)
        {
            Progress = progress;
        }

        /// <summary>The Game scene's own load progress; the Loading screen's bar reads it.</summary>
        internal ISessionLoadProgress Progress { get; }

        internal void Open()
        {
            answer.TrySetResult(true);
        }

        internal void Fail()
        {
            answer.TrySetResult(false);
        }

        /// <summary>True once the scene is ready to play, false when it failed. The first answer stays.</summary>
        internal UniTask<bool> WaitAsync()
        {
            return answer.Task;
        }
    }
}
