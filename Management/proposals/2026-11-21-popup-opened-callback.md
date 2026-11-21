# Đề xuất: Popup báo lại object vừa mở, để bỏ `ShopRuntime`

Ngày: 2026-11-21. Trạng thái: chờ duyệt. Chưa có dòng code nào được sửa.

## Vì sao cần

`unity.md` giờ cấm static giữ trạng thái kể cả trong nội bộ feature. Theo `Shop/CLAUDE.md`, view của Shop "lấy logic qua `ShopRuntime`", nên Shop đang phạm luật này.

Wallet đã bỏ được static vì chính Wallet tạo view của mình, nên đưa logic vào ngay lúc tạo. Popup thì khác: dịch vụ Popup tạo object từ prefab, và có thể tạo muộn vì popup phải xếp hàng. `IPopupService.Show` chỉ trả `PopupHandle`, nên feature gọi không bao giờ cầm được object đó. Muốn đưa dữ liệu hay logic vào popup thì chỉ còn cách để popup tự tìm qua static.

## Giới hạn của đề xuất này

Tôi viết từ Contracts của Popup và `CLAUDE.md` của Shop; Internal và View của hai feature đó nằm ngoài vùng được đọc. Vì vậy có một điều chưa kiểm: `ShopRuntime` được dùng ở đâu.

- Nếu chỉ view mua hàng của Shop dùng nó (view do Shop tự quản), thì **không cần đổi contract Popup**: phần A bên dưới là đủ.
- Nếu `PurchasedPopup.prefab` cũng dùng nó, thì cần cả phần B.

Người làm Shop (phiên `FEATURE=Shop`) xác nhận điều này trước khi bắt tay vào phần B.

## Phần A: view của chính Shop (không đụng Popup)

Làm như Wallet:

- View mua hàng chỉ hiện thực một interface internal của Shop và chuyển thao tác "mua" xuống presenter. Nó không biết `ShopLogic`.
- Một presenter trong Internal tham gia `ISessionScene`: tạo view trong `Run`, huỷ trong `DisposeAsync`.
- Installer kiểm config và prefab ngay đầu `Install`.

## Phần B: đổi contract Popup

Thêm vào `PopupRequest` một callback tuỳ chọn, gọi khi dịch vụ vừa tạo object của popup:

```csharp
public sealed class PopupRequest
{
    public PopupRequest(GameObject prefab) : this(prefab, null) { }

    public PopupRequest(GameObject prefab, Action<GameObject> opened)
    {
        Prefab = prefab;
        Opened = opened;
    }

    public GameObject Prefab { get; }

    /// Gọi đúng một lần, ngay sau khi popup được tạo và trước khi người chơi thấy nó.
    /// Không gọi nếu popup bị huỷ khi còn xếp hàng. Null thì bỏ qua.
    public Action<GameObject> Opened { get; }
}
```

`IPopupService` và `PopupHandle` không đổi.

### Vì sao chọn cách này

- **Chỉ thêm, không đổi.** Constructor cũ còn nguyên, nên code Shop hiện tại vẫn biên dịch và chạy như cũ. Điều này khớp luật "contract đã có feature khác dùng thì không đổi chữ ký".
- **Popup vẫn không biết feature nào.** Nó chỉ gọi lại một hàm và đưa object nó vừa tạo.
- **Khớp với hàng đợi.** Popup xếp hàng thì chưa có object; callback chạy đúng lúc object ra đời.

### Hai cách đã cân nhắc và bỏ

| Cách | Vì sao bỏ |
|---|---|
| Prefab hiện thực một interface của Popup, yêu cầu mang theo một `object` dữ liệu | Dữ liệu mất kiểu, feature gọi phải ép kiểu; Popup phải biết thêm một interface mà mọi prefab đều phải gắn |
| Feature tự tạo object rồi giao cho Popup hiển thị | Đổi nghĩa của contract đang có ("dịch vụ tạo object"); object phải tồn tại và bị ẩn suốt lúc xếp hàng |

### Shop dùng thế nào

- Khi mua xong, Shop gọi `Show(new PopupRequest(prefab, opened))`.
- Trong `opened`, Shop lấy view của mình trên object đó và đưa dữ liệu vào (tên vật phẩm, hành động đóng...). Việc lấy component nằm sau một interface trong Internal của Shop, để test dùng đồ giả.
- Xoá `ShopRuntime`.

## Những chỗ cần người duyệt

| Việc | Vì sao phải hỏi |
|---|---|
| Sửa `PopupRequest.cs` trong Contracts của Popup | Contracts của mình cũng phải hỏi; đây là contract đang có feature dùng |
| Thêm tham chiếu SessionScene Contracts và UniTask vào asmdef Internal và Tests của Shop (phần A) | sửa `*.asmdef` phải hỏi |
| Xoá `ShopRuntime.cs` | phiên Shop xoá qua unity-mcp; không cần hỏi nếu file nằm trong Internal của Shop |

Không cần sửa `rules.json`: ngoại lệ ASM-05 hiện có đã phủ toàn bộ Contracts của Popup, và `System.Action` không kéo thêm tham chiếu nào.

## Test cần có

- Popup: callback được gọi đúng một lần với đúng object vừa tạo; popup xếp hàng thì callback chỉ chạy khi tới lượt; popup bị huỷ khi còn xếp hàng thì không gọi; yêu cầu không có callback vẫn chạy như cũ.
- Shop: mua xong thì popup nhận đúng dữ liệu, dùng dịch vụ Popup giả gọi lại callback ngay trong test.

## Thứ tự làm đề nghị

1. Phiên `FEATURE=Shop`: xác nhận `ShopRuntime` được dùng ở đâu; làm phần A.
2. Nếu cần phần B: phiên `FEATURE=_Services/Popup` thêm callback và test.
3. Phiên `FEATURE=Shop`: dùng callback, xoá `ShopRuntime`, sửa `Shop/CLAUDE.md`.

## Câu hỏi cần người quyết

1. **Callback ném lỗi thì sao?** Đề nghị: Popup ghi lỗi, đóng popup đó và cho hàng đợi đi tiếp, để một popup hỏng không chặn các popup sau.
2. **Có cần báo cả lúc popup đóng không?** Đề nghị chưa thêm: hiện chưa feature nào cần, và luật không cho mở contract "phòng khi cần".
3. **Ngoại lệ về hình thức contract.** Luật ghi contract chỉ gồm interface, enum, struct chỉ đọc; `PopupRequest` vốn là lớp chỉ đọc và giờ mang thêm một delegate. Đề nghị chấp nhận, vì delegate ở đây là dữ liệu của yêu cầu, không phải logic của Popup.
