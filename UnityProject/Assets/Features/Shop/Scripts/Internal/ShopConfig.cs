using System.Collections.Generic;
using UnityEngine;

namespace Game.Shop
{
    /// <summary>
    /// The catalogue and the shop's buy-view prefab. Read-only at runtime. The purchased popup is not here:
    /// it goes to the Popup service by id (<see cref="ShopPopups"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Shop/Config")]
    internal sealed class ShopConfig : ScriptableObject
    {
        [SerializeField] private ShopItem[] items = new ShopItem[0];
        [SerializeField] private GameObject buyViewPrefab;

        internal IReadOnlyList<ShopItem> Items => items;
        internal GameObject BuyViewPrefab => buyViewPrefab;
    }
}
