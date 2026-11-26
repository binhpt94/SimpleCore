using System.Globalization;

namespace Game.ButtonTest
{
    /// <summary>Chữ trên nút và chữ của dòng số đếm (button-spec#button-text, #counter-text).</summary>
    internal static class CounterText
    {
        internal const string ButtonLabel = "Thử";

        private const string CounterPrefix = "Đã bấm: ";

        /// <summary>"Đã bấm: N", ghi đủ số, không rút gọn, không dấu phân cách (ví dụ "Đã bấm: 1250").</summary>
        internal static string Format(long count) => CounterPrefix + count.ToString(CultureInfo.InvariantCulture);
    }
}
