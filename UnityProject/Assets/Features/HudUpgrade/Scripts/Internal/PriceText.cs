namespace Game.HudUpgrade
{
    /// <summary>
    /// How a price reads on a button: in full below 10.000, then cut down (never rounded up) to one decimal
    /// with a Vietnamese decimal comma: 12.399 reads "12,3K", 1.250.000 reads "1,2M".
    /// </summary>
    internal static class PriceText
    {
        private const int ShortFrom = 10_000;
        private const int MillionFrom = 1_000_000;

        public static string Format(int price)
        {
            if (price < ShortFrom) return price.ToString();
            if (price < MillionFrom) return Tenths(price / 100) + "K";
            return Tenths(price / 100_000) + "M";
        }

        private static string Tenths(int tenths) => (tenths / 10) + "," + (tenths % 10);
    }
}
