using System.Collections.Generic;
using UnityEngine;

namespace Game.Shop
{
    /// <summary>The catalogue and the shop's own popup prefab. Read-only at runtime.</summary>
    [CreateAssetMenu(menuName = "Game/Shop/Config")]
    internal sealed class ShopConfig : ScriptableObject
    {
        [SerializeField] private ShopItem[] items = new ShopItem[0];
        [SerializeField] private GameObject purchasedPopupPrefab;

        internal IReadOnlyList<ShopItem> Items => items;
        internal GameObject PurchasedPopupPrefab => purchasedPopupPrefab;
    }
}
