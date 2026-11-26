using System;

namespace Game.ButtonTest
{
    /// <summary>Nút "Thử" và dòng số đếm trên màn trận. View chỉ hiển thị và báo lần nhả chạm.</summary>
    internal interface ITestButtonView
    {
        /// <summary>Object Unity gốc của view, để phiên đếm view đang giữ.</summary>
        UnityEngine.Object Root { get; }

        /// <summary>Báo một lần nhả chạm; true khi nhả trên nút, false khi đã kéo ra ngoài nút.</summary>
        event Action<bool> Released;

        /// <summary>Đặt chỗ, cỡ và thứ tự vẽ theo config; gọi một lần ngay sau khi tạo.</summary>
        void ApplyLayout(TestButtonLayout layout, string buttonLabel);

        void SetVisible(bool visible);

        void SetCounterText(string text);
    }

    /// <summary>Tạo và huỷ view (đụng Unity), để test thay bằng đồ giả.</summary>
    internal interface ITestButtonViewFactory
    {
        ITestButtonView Create();

        void Destroy(ITestButtonView view);
    }
}
