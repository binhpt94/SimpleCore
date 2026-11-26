namespace Game.ButtonTest
{
    /// <summary>State dựng nhanh của node feature-button-test/cheat. Chỉ có tác dụng ở bản dev.</summary>
    internal enum ButtonTestCheat
    {
        None = 0,

        /// <summary>cheat#pressed-12: nút "Thử" hiện trên màn trận với nhãn "Đã bấm: 12"; bấm tiếp thì tăng từ 12.</summary>
        Pressed12 = 1,
    }

    internal static class ButtonTestCheatValues
    {
        /// <summary>N ép trong state pressed-12.</summary>
        internal const long Pressed12Count = 12;
    }
}
