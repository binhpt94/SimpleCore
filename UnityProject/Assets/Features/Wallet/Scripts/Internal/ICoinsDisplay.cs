namespace Game.Wallet
{
    /// <summary>Something on screen that shows the coin count. It shows what it is told and decides nothing.</summary>
    internal interface ICoinsDisplay
    {
        void ShowCoins(int coins);

        void Close();
    }

    /// <summary>Creates the display. Behind an interface so tests need no Unity objects.</summary>
    internal interface ICoinsDisplaySource
    {
        ICoinsDisplay Create();
    }
}
