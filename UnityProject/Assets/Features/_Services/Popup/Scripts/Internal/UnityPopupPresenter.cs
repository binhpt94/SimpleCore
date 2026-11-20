using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>Instantiates popups under one root object that survives scene loads.</summary>
    internal sealed class UnityPopupPresenter : IPopupPresenter
    {
        private Transform root;

        public GameObject Present(GameObject prefab)
        {
            if (root == null)
            {
                var rootObject = new GameObject("PopupRoot");
                Object.DontDestroyOnLoad(rootObject);
                root = rootObject.transform;
            }
            return Object.Instantiate(prefab, root);
        }

        public void Dismiss(GameObject instance)
        {
            if (instance != null) Object.Destroy(instance);
        }
    }
}
