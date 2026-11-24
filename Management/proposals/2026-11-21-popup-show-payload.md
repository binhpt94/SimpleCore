# Đề xuất: Popup nhận dữ liệu, `Show(id, payload)`

Ngày: 2026-11-21. Trạng thái: chờ duyệt. Chưa có dòng code nào được sửa.
Đề xuất này làm chi tiết mục 4 của `2026-11-21-contracts-thuan-va-id-asset.md` và sửa một chỗ trong mục đó không làm được (xem "Chỗ mục 4 vướng luật").

## Vì sao cần

`IPopupService` hiện chỉ có `Show(PopupId)`, nên popup chỉ hiện được nội dung cố định có sẵn trong prefab. Popup cần dữ liệu lúc chạy (tên vật phẩm vừa mua, số xu còn thiếu) thì không có đường nào đưa vào. Đường duy nhất còn lại là static, mà `unity.md` cấm.

**Chưa có feature nào xin dùng.** Theo luật "không mở contract phòng khi cần", chỉ làm khi có bên dùng thật. Câu hỏi 1 bên dưới hỏi ai là bên dùng đầu tiên.

## Chỗ mục 4 vướng luật

Mục 4 đặt `IPopupContent<TPayload>` trong Contracts của Popup, và cho view của feature gọi hiện thực interface đó. Nhưng ma trận tham chiếu trong `validate.py` (`check_reference`, nhánh `View`) chỉ cho View tham chiếu **Internal và Contracts của chính nó, cùng `Game.Core`**. Ví dụ View của Shop không tham chiếu được `Game.Services.Popup.Contracts`, nên không hiện thực được interface nằm ở đó. Lỗi sẽ là ASM-04.

Lớp chuyển dữ liệu cũng không đặt được trong Internal của feature gọi: Popup tìm nội dung bằng component trên object vừa tạo, mà Internal không được có MonoBehaviour (SRC-02).

### Các cách gỡ

| Cách | Đổi gì | Đánh giá |
|---|---|---|
| **A. Interface chung trong Core** (đề nghị) | Thêm `Game.Core.IPayloadView<T>` vào `Core`. Popup và View của feature gọi đều thấy được Core. | Interface thuần, chung chung, không có luật game, không biết tên feature. Giống cách đã làm với `AssetCatalog`. Dịch vụ khác cũng dùng lại được (Toast, Tutorial). Phải hỏi vì sửa `Core`. |
| B. Cho View tham chiếu Contracts của dịch vụ | Sửa ma trận trong `validate.py` | Đổi luật ranh giới cho mọi feature, rộng hơn mức cần. Không thuộc quyền AI. |
| C. Payload kiểu `object`, Popup dò component bằng reflection/`SendMessage` | Không đổi luật | Mất kiểu và lách luật. Bỏ. |

Phần dưới viết theo cách A.

## Contract

```csharp
// Core/IPayloadView.cs (mới, cần duyệt vì sửa Core)
namespace Game.Core
{
    /// Một view nhận dữ liệu do bên khác đưa tới. Chỉ hiển thị, không quyết định gì.
    public interface IPayloadView<in T> where T : struct
    {
        void Show(T payload);
    }
}
```

```csharp
// Popup/Scripts/Contracts/IPopupService.cs: thêm một hàm, giữ nguyên hai hàm cũ
PopupHandle Show<TPayload>(PopupId id, TPayload payload) where TPayload : struct;
```

- Chỉ thêm, không đổi `Show(PopupId)` hay `Close`. Shop đang dùng chúng nên vẫn biên dịch như cũ.
- Contracts của Popup vẫn thuần C# (`noEngineReferences: true`). Không cần ngoại lệ trong `rules.json`.
- Payload là struct chỉ đọc do feature gọi định nghĩa trong Internal của nó. Popup không biết kiểu cụ thể.

## Hành vi

- **Kiểm ngay lúc gọi `Show`:** id phải có trong catalog, như hiện nay. Gốc prefab phải có component hiện thực `IPayloadView<TPayload>`. Thiếu thì ném lỗi ngay, nêu id và tên kiểu payload, không chờ tới lượt mở mới lộ.
- **Lúc đưa dữ liệu:** ngay sau khi tạo object, trong cùng frame, trước khi người chơi thấy. `Awake`/`OnEnable` của view chạy trước khi có dữ liệu, nên view phải hiện dữ liệu trong `Show(payload)`, không làm trong `Awake`.
- **Xếp hàng:** payload được giữ cùng mục trong hàng đợi và chỉ đưa vào khi tới lượt. Popup bị `Close` khi còn chờ thì không tạo object và không đưa dữ liệu.
- Popup vẫn không trả instance Unity về cho feature gọi.

## Sửa trong Popup

- `IPopupPresenter` thêm hai việc để test thay bằng đồ giả: kiểm prefab có nhận payload kiểu `T` không, và đưa payload vào object đã tạo. Bản thật (`UnityPopupPresenter`) dùng `GetComponent<IPayloadView<T>>()` ở gốc.
- `PopupService.Entry` giữ thêm một bước "đưa dữ liệu" (một delegate dựng lúc `Show`, trong Internal). `Show(PopupId)` cũ dùng bước rỗng.
- Cập nhật `Popup/CLAUDE.md`.

## Test (EditMode, `Game.Services.Popup.Tests`)

- Mở ngay: object vừa tạo nhận đúng payload, đúng một lần.
- Đang có popup mở: payload chưa được đưa vào; tới khi popup trước đóng thì mới đưa.
- Huỷ khi còn chờ: không tạo object, không đưa payload.
- Prefab không nhận được kiểu payload đó: `Show` ném lỗi ngay, thông báo nêu id và tên kiểu.
- `Show(PopupId)` không payload vẫn chạy như cũ.

## Những chỗ cần người duyệt

| Việc | Vì sao phải hỏi |
|---|---|
| Thêm `Core/IPayloadView.cs` (cách A) | Sửa `Core` phải hỏi. Không làm được trong Popup vì View của feature gọi chỉ thấy Core |
| Thêm `Show<TPayload>` vào `IPopupService` | Contracts của mình cũng phải hỏi; đây là contract đã có Shop dùng (chỉ thêm, không đổi) |
| Sửa `.claude/rules/unity.md` mục "Asset giao cho dịch vụ": thay `IPopupContent<T>` bằng `IPayloadView<T>` của Core, nếu mục đó đang nhắc | `.claude/**` chỉ người sửa |

Không cần sửa asmdef nào: Contracts, Internal của Popup và View của feature gọi đều đã tham chiếu `Game.Core`.

## Câu hỏi cần người quyết

1. **Bên dùng đầu tiên là ai?** Ví dụ Shop hiện tên vật phẩm trong popup "đã mua". Chưa có bên dùng thì đề nghị duyệt hướng đi nhưng chưa làm.
2. **Chọn A hay B** cho chỗ vướng luật.
3. **Popup tự đóng bằng nút trên nó thì sao?** Hiện chỉ bên giữ `PopupHandle` gọi `Close` được. Nếu view cần nút "OK" tự đóng, đó là việc riêng, đề nghị viết đề xuất khác khi có nhu cầu.
