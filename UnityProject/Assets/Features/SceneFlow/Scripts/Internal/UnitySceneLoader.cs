using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Game.SceneFlow
{
    internal sealed class UnitySceneLoader : ISceneLoader
    {
        public async UniTask LoadReplacingAsync(string sceneName)
        {
            // Single mode removes every open scene once the new one is in. The old scene's session has already
            // ended by then (design: destroy the old scene first, then load the new one).
            var loading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (loading == null)
            {
                throw new InvalidOperationException(
                    $"Scene '{sceneName}' cannot be loaded: check its name and that it is in Build Settings.");
            }

            await loading.ToUniTask();
        }

        public void SetActive(string sceneName)
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        }

        public SceneGate FindGate()
        {
            // The carrier is created while its scene is still loading, so Unity may put it in whichever
            // scene is active at that moment. Looking through every loaded scene does not depend on that.
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    var carrier = root.GetComponent<ISceneGateCarrier>();
                    if (carrier != null && carrier.Gate != null) return carrier.Gate;
                }
            }

            return null;
        }
    }
}
