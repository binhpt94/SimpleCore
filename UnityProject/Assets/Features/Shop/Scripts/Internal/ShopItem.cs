using System;
using UnityEngine;

namespace Game.Shop
{
    /// <summary>One thing the shop sells.</summary>
    [Serializable]
    internal struct ShopItem
    {
        [SerializeField] private string id;
        [SerializeField] private int price;

        internal ShopItem(string id, int price)
        {
            this.id = id;
            this.price = price;
        }

        internal string Id => id;
        internal int Price => price;
    }
}
