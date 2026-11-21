namespace Game.Shop
{
    /// <summary>Creates and destroys the buy view, so tests can stand in for Unity.</summary>
    internal interface IShopBuyViewSource
    {
        /// <summary>Creates the view. Called at most once before <see cref="Close"/>.</summary>
        IShopBuyView Open();

        /// <summary>Destroys the view made by <see cref="Open"/>.</summary>
        void Close();
    }
}
