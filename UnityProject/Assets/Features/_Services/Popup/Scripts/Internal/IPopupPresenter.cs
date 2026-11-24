using UnityEngine;

namespace Game.Services.Popup
{
    /// <summary>
    /// Puts popups on screen and takes them off. Separated so the stack logic can be tested without a scene.
    /// Each popup presented goes on top of all earlier ones, with its own backdrop right behind it.
    /// </summary>
    internal interface IPopupPresenter
    {
        /// <summary>Creates the popup and its backdrop on top of everything shown so far; returns the popup's root.</summary>
        GameObject Present(GameObject prefab);

        /// <summary>Removes the popup and its backdrop. The others keep their order.</summary>
        void Dismiss(GameObject instance);

        /// <summary>Removes every popup and the layer itself. Used when the scene's session ends.</summary>
        void DismissAll();
    }
}
