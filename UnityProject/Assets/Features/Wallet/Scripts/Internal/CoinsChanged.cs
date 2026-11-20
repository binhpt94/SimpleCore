namespace Game.Wallet
{
    /// <summary>Raised after the coin count changed. Carries both values so listeners need not ask again.</summary>
    internal readonly struct CoinsChanged
    {
        internal CoinsChanged(int previous, int current)
        {
            Previous = previous;
            Current = current;
        }

        internal int Previous { get; }
        internal int Current { get; }
    }
}
