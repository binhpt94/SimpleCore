using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>Puts a popup on screen and takes it off. Separated so the queue logic can be tested without a scene.</summary>
    internal interface IPopupPresenter
    {
        GameObject Present(GameObject prefab);

        void Dismiss(GameObject instance);
    }
}
