namespace Game.Castle
{
    /// <summary>The castle parts that can be levelled during a run.</summary>
    public enum CastleUpgrade
    {
        Health,
        Attack,
        Skills,
    }

    /// <summary>Receives levels bought for the castle. Prices and coins are not the castle's business.</summary>
    public interface ICastleUpgrades
    {
        /// <summary><paramref name="item"/> is now at <paramref name="level"/>; the castle applies the effect at once.</summary>
        void ApplyLevel(CastleUpgrade item, int level);
    }
}
