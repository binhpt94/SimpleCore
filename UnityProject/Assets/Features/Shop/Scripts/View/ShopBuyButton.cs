using UnityEngine;
using UnityEngine.UI;

namespace Game.Shop
{
    /// <summary>Sends "buy this item" to the shop logic when clicked. Decides nothing itself.</summary>
    internal sealed class ShopBuyButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private string itemId;

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(Buy);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(Buy);
        }

        private void Buy()
        {
            ShopRuntime.Logic?.TryPurchase(itemId);
        }
    }
}
