using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shop
{
    /// <summary>Reports "buy this item" when clicked. Decides nothing itself.</summary>
    internal sealed class ShopBuyButton : MonoBehaviour, IShopBuyView
    {
        [SerializeField] private Button button;
        [SerializeField] private string itemId;

        public event Action<string> BuyRequested;

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
            BuyRequested?.Invoke(itemId);
        }
    }
}
