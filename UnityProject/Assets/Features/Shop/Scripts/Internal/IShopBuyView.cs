using System;

namespace Game.Shop
{
    /// <summary>What the shop needs from its buy view: tell us which item the player asked to buy.</summary>
    internal interface IShopBuyView
    {
        /// <summary>Raised with the item id each time the player asks to buy.</summary>
        event Action<string> BuyRequested;
    }
}
