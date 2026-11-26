using System;
using UnityEngine;

namespace Game.ButtonTest
{
    /// <summary>
    /// Chỗ của nút "Thử" và dòng số đếm theo frame của @UIDesigner (px trên màn chuẩn 1920x1080, gốc ở góc trên trái),
    /// và thứ tự vẽ của canvas. Đổi frame thì sửa asset ButtonTestConfig ở FeatureAssets/Private, không sửa code.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/ButtonTest/Config", fileName = "ButtonTestConfig")]
    internal sealed class ButtonTestConfig : ScriptableObject
    {
        /// <summary>Thứ tự vẽ của lớp popup (canvas overlay của dịch vụ Popup, ghi ở CLAUDE.md của nó).</summary>
        internal const int PopupLayerSortingOrder = 1000;

        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

        // Frame main.frame của button-spec trên live: góc trái dưới dải chiến trường, đáy nút ngay trên mép dưới HUD (y=900),
        // nhãn số đếm bên phải nút, cùng hàng.
        [SerializeField] private FrameRect button = new FrameRect(40f, 756f, 200f, 120f);
        [SerializeField] private FrameRect counter = new FrameRect(264f, 786f, 300f, 60f);

        [Tooltip("Phải nhỏ hơn thứ tự vẽ của lớp popup (1000) để popup che và chặn chạm.")]
        [SerializeField] private int canvasSortingOrder = 100;

        internal TestButtonLayout Layout => new TestButtonLayout(referenceResolution, button, counter, canvasSortingOrder);

        /// <summary>Ném lỗi nêu đúng thứ sai, để Install dừng ngay.</summary>
        internal void Validate()
        {
            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
                throw new InvalidOperationException($"ButtonTestConfig '{name}': referenceResolution phải dương.");
            if (button.Width <= 0f || button.Height <= 0f)
                throw new InvalidOperationException($"ButtonTestConfig '{name}': kích thước nút phải dương.");
            if (counter.Width <= 0f || counter.Height <= 0f)
                throw new InvalidOperationException($"ButtonTestConfig '{name}': kích thước nhãn số đếm phải dương.");
            if (canvasSortingOrder >= PopupLayerSortingOrder)
                throw new InvalidOperationException(
                    $"ButtonTestConfig '{name}': canvasSortingOrder {canvasSortingOrder} phải nhỏ hơn lớp popup ({PopupLayerSortingOrder}).");
        }
    }

    /// <summary>Một hình chữ nhật của frame: x, y tính từ góc trên trái màn chuẩn, đơn vị px.</summary>
    [Serializable]
    internal struct FrameRect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public FrameRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>Bố cục đưa xuống view; view chỉ đặt theo, không tính gì thêm.</summary>
    internal readonly struct TestButtonLayout
    {
        public readonly Vector2 ReferenceResolution;
        public readonly FrameRect Button;
        public readonly FrameRect Counter;
        public readonly int SortingOrder;

        public TestButtonLayout(Vector2 referenceResolution, FrameRect button, FrameRect counter, int sortingOrder)
        {
            ReferenceResolution = referenceResolution;
            Button = button;
            Counter = counter;
            SortingOrder = sortingOrder;
        }
    }
}
