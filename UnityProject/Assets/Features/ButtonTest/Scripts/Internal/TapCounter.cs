using System;

namespace Game.ButtonTest
{
    /// <summary>
    /// Số lần đã bấm nút "Thử" (N) kể từ lúc màn trận mở.
    /// Một lần chạm rồi nhả trên nút thì N tăng 1 ngay trong lời gọi đó (cùng khung hình);
    /// nhả ở ngoài nút thì không tính. Không lưu, không đọc hay gửi gì cho feature khác.
    /// </summary>
    internal sealed class TapCounter
    {
        /// <summary>Mức tăng mỗi lần chạm (thông số của button-spec).</summary>
        internal const int StepPerTap = 1;

        /// <summary>N, bắt đầu từ 0.</summary>
        internal long Count { get; private set; }

        /// <summary>Gọi ngay khi N đổi, với N mới, để nhãn đổi chữ trong cùng khung hình.</summary>
        internal event Action<long> Changed;

        /// <summary>Đưa N về 0 (màn trận mở lại) và báo <see cref="Changed"/>.</summary>
        internal void Reset()
        {
            Count = 0;
            Changed?.Invoke(Count);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Cheat (chỉ bản dev): ép N về giá trị của state, báo <see cref="Changed"/>.</summary>
        internal void ForceCount(long count)
        {
            Count = count;
            Changed?.Invoke(Count);
        }
#endif

        /// <summary>Một lần nhả chạm trên nút thử. <paramref name="releasedOverButton"/> sai là đã kéo ra ngoài nút rồi mới nhả.</summary>
        /// <returns>Có tính lần này không.</returns>
        internal bool Release(bool releasedOverButton)
        {
            if (!releasedOverButton)
            {
                return false;
            }

            Count += StepPerTap;
            Changed?.Invoke(Count);
            return true;
        }
    }
}
