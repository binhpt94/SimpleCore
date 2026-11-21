# Đề xuất: Contracts luôn thuần C#, asset giao cho dịch vụ qua id

Ngày: 2026-11-21. Trạng thái: chờ duyệt. Chưa có dòng code nào được sửa.

## Vì sao cần

Luật `ASM-05` bắt Contracts đặt `noEngineReferences: true`. Popup đã phải xin ngoại lệ ngay từ đầu, vì `PopupRequest` mang `GameObject` (prefab do feature gọi đưa tới). Game lớn sẽ có thêm nhiều dịch vụ nhận asset từ feature gọi: Audio, VFX, Pool, Toast, Tutorial. Nếu dịch vụ nào cũng truyền object Unity qua contract, `rules.json` sẽ thành một danh sách ngoại lệ dài và luật mất ý nghĩa.

Mục tiêu:

- Contracts của mọi feature và dịch vụ là C# thuần, không có ngoại lệ ASM-05 nào.
- Asset đi qua một id thuần. Nơi duy nhất biết id ứng với asset nào là `_Composition`.

## Giới hạn

- Viết từ Contracts và Internal của Popup, Internal của Shop, `validate.py` và `rules.json`. Chưa thử trong Unity.
- Shop là feature duy nhất đang gọi Popup: `ShopLogic` giữ `GameObject purchasedPopupPrefab` và gọi `popups.Show(new PopupRequest(purchasedPopupPrefab))`. Prefab `PurchasedPopup.prefab` đang nằm ở `Shop/FeatureAssets/Private`.

## 1. Contracts luôn thuần C#

- Giữ nguyên `ASM-05`. Mục tiêu là xoá ngoại lệ ASM-05 duy nhất trong `rules.json` (của Popup).
- Chưa làm tầng `Contracts.Unity` (Contracts được dùng một danh sách kiểu Unity cố định như `Vector3`, `Color`). Hiện chưa feature nào cần. Khi có nhu cầu thật thì viết đề xuất riêng.

## 2. Định dạng id

```
<Owner>/<Name>
```

- `Owner`: id của feature sở hữu asset, giống hệt giá trị gõ sau `FEATURE=`. Ví dụ `Shop`, `_Services/Popup`.
- `Name`: PascalCase, có thể chia cấp bằng dấu chấm. Ví dụ `BuyConfirm`, `Buy.Confirm`.
- Regex: `^(_Services/)?[A-Z][A-Za-z0-9]*/[A-Z][A-Za-z0-9]*(\.[A-Z][A-Za-z0-9]*)*$`
- Ví dụ: `Shop/Purchased`, `Wallet/NotEnoughCoins`, `Shop/Buy.Confirm`.

Vì tiền tố là tên feature, hai feature không thể trùng id. Trùng trong cùng một feature thì test catalog bắt (mục 5).

Mỗi loại id là một kiểu riêng do dịch vụ định nghĩa trong Contracts của nó, ví dụ `PopupId`, `SoundId`. Kiểu đã phân biệt loại, nên không ghi loại vào chuỗi: `Shop/Click` vừa là âm thanh vừa là popup cũng không lẫn.

```csharp
// _Services/Popup/Scripts/Contracts/PopupId.cs
public readonly struct PopupId : IEquatable<PopupId>
{
    public PopupId(string value) { /* ném ArgumentException nếu sai regex */ Value = value; }
    public string Value { get; }
    // Equals, GetHashCode, ToString => Value
}
```

Feature sở hữu asset khai báo id đúng một lần, trong Contracts của nó:

```csharp
// Shop/Scripts/Contracts/ShopPopups.cs
public static class ShopPopups
{
    public static readonly PopupId Purchased = new PopupId("Shop/Purchased");
}
```

- Nơi khác chỉ dùng hằng (`ShopPopups.Purchased`), không gõ lại chuỗi.
- `static readonly` chỉ giữ giá trị bất biến, không phải trạng thái, nên không phạm luật "không static giữ trạng thái". Câu này sẽ được ghi vào `.claude/rules/unity.md`.
- Id đã có nơi dùng là contract: không đổi chuỗi, không xoá. Cần đổi thì đề xuất.
- Không băm chuỗi, để id đọc được trong log và catalog.

Hệ quả cho Shop: Shop chưa có Contracts. Theo chuẩn này Shop sẽ có `Scripts/Contracts` (asmdef `Game.Shop.Contracts`) chỉ để chứa `ShopPopups`. Contracts này chỉ là danh sách id, không mở hành vi nào của Shop.

## 3. Catalog theo scene, do `_Composition` giữ

Asset đi theo scene: scene Loading và scene Game cần những popup khác nhau. Vì vậy mỗi scene có catalog riêng.

### 3a. Boot theo scene: prefab variant

Hiện tại chỉ có một `Boot.prefab`. Mỗi scene đặt một bản của nó rồi override danh sách `installers` ngay trong file scene:

| | Danh sách installer |
|---|---|
| `Boot.prefab` | SessionScene, Popup, Wallet, Shop |
| `Loading.unity` (override: size 2, `data[1]`) | SessionScene, SceneFlowLoading |
| `Game.unity` (override: size 5, `data[4]`) | SessionScene, Popup, Wallet, Shop, SceneFlowGame |

Cách này có ba điểm yếu: override nằm lẫn trong YAML của scene nên khó duyệt; override theo chỉ số mảng nên sửa prefab gốc (chèn một installer) làm danh sách của từng scene đổi ngầm; và test phải mở scene mới kiểm được.

Đổi thành:

- `Boot.prefab` gốc: danh sách installer rỗng, catalog rỗng.
- `Boot.Loading.prefab`, `Boot.Game.prefab`: prefab variant của `Boot.prefab`, mỗi cái ghi đủ danh sách installer và catalog của scene đó.
- Mỗi scene đặt đúng variant của nó, không có override nào trong file scene.
- `BootTests` nạp từng variant: thứ tự cài đúng luật (SessionScene đầu, bên SceneFlow của scene đó cuối), catalog hợp lệ.

### 3b. Catalog trong variant, nạp và nhả qua SessionScene

- Asset giao cho dịch vụ nằm ở `FeatureAssets/Public` của feature sở hữu. Đây đã là contract: đổi tên hay xoá phải hỏi. Với Shop: chuyển `PurchasedPopup.prefab` từ `Private` sang `Public`, giữ nguyên GUID.
- Popup định nghĩa kiểu asset catalog `PopupCatalog` (danh sách cặp id → prefab) trong Internal của nó, đọc qua một interface Internal để test thay bằng đồ giả.
- Mỗi variant của Boot có một trường catalog. `Boot` đăng ký catalog vào `IServiceRegistry` trước khi chạy các installer; Popup lấy nó bằng `Resolve` trong `Install`. Nhờ vậy asset của Popup không phải tham chiếu ngược lên `_Composition`.
- Thời điểm nạp và nhả đi qua SessionScene, như mọi feature khác: Popup gọi `Join` trong `Install`, nạp asset của catalog trong `LoadAsync`, nhả trong `DisposeAsync`. Khi chuyển sang Addressables, đây là chỗ gọi load và release; catalog chỉ đổi prefab thành key hoặc `AssetReference`, contract không đổi.
- Gọi `Show` với id không có trong catalog của scene hiện tại: lỗi ngay, thông báo nêu id và tên scene. Không âm thầm bỏ qua.

Vì sao không để chính dịch vụ SessionScene giữ catalog:

- `AST-01` cấm dịch vụ tham chiếu asset của feature ("Core and services may not reference any feature's assets").
- SessionScene chỉ giữ vòng đời phiên và "không biết feature nào"; giữ catalog popup là biết về Popup và id của từng feature.

Đã kiểm: `AST-01` cũng báo khi `_Composition` tham chiếu `FeatureAssets/Private` của feature, nên không cần luật mới cho chỗ này.

## 4. Đưa dữ liệu vào popup

- `IPopupService.Show(PopupId id)` cho popup không cần dữ liệu. Popup có nội dung thì `Show<TPayload>(PopupId id, TPayload payload)`, payload là struct thuần do feature gọi định nghĩa.
- Prefab popup mang một view của feature gọi. View này nhận payload qua một interface trong Internal của feature gọi; Popup chỉ chuyển payload tới component hiện thực interface chung `IPopupContent<TPayload>` đặt trong Contracts của Popup.
- Popup chỉ lo hàng đợi, mở và đóng. Không bao giờ trả instance Unity về cho feature gọi.

Phần này thay cho phần B của đề xuất `2026-11-21-popup-opened-callback.md` (callback trả object vừa mở), đề xuất đó vẫn đang chờ duyệt. Bạn chọn một trong hai. Phần A của đề xuất kia (view mua của Shop) không bị ảnh hưởng.

## 5. Kiểm tra tự động

Mất kiểm tra lúc biên dịch (gõ sai id chỉ lộ lúc chạy), nên bù bằng:

| Kiểm | Ở đâu |
|---|---|
| Chuỗi đúng regex | Constructor của id; validator |
| Tiền tố `Owner` trùng feature chứa file | Validator: trong Shop chỉ được tạo id `Shop/...` |
| `new XxxId("...")` chỉ nằm trong `Scripts/Contracts` | Validator |
| Không trùng id, id nào cũng có asset, không có asset thừa | Test EditMode của mỗi dịch vụ nạp catalog thật |
| Thiếu catalog hoặc catalog rỗng | `Install` của dịch vụ lỗi ngay, nêu tên thứ bị thiếu |

Luật validator mới, tạm gọi `SRC-05`: quét `new <Tên>Id("<chuỗi>")` (tên kiểu kết thúc bằng `Id`) trong mọi file `.cs` của feature và dịch vụ, báo ba lỗi đầu của bảng. Chỉ nhận chuỗi viết thẳng; tạo id từ biến hay ghép chuỗi trong Contracts cũng bị báo, để luật không bị lách.

## 6. Đổi luật trong `.claude/rules/unity.md`

Thêm mục "Asset giao cho dịch vụ":

- Dịch vụ nhận asset qua id, không qua object Unity. Id dạng `<Owner>/<Name>`, khai báo một lần trong Contracts của chủ.
- `static readonly` cho id là giá trị bất biến, được phép.
- Asset giao cho dịch vụ nằm ở `FeatureAssets/Public` của chủ; catalog nằm ở `_Composition`.

Mục "Prefab giao cho dịch vụ (ví dụ Popup) là của feature gọi, đặt trong feature gọi" giữ nguyên, chỉ thêm "ở `FeatureAssets/Public`".

## 7. Lộ trình

Mỗi bước duyệt riêng; bước nào đụng vùng "hỏi người" thì hỏi từng lần.

1. Duyệt đề xuất này, và chọn giữa mục 4 và phần B của đề xuất popup-opened-callback.
2. Thêm `SRC-05` vào `validate.py`, kèm test trong `test_validate.py`.
3. `_Composition`, Boot theo scene: tạo `Boot.Loading.prefab` và `Boot.Game.prefab` (variant), chuyển danh sách installer từ override trong scene sang variant, mỗi scene đặt variant của nó. `BootTests` kiểm từng variant. Bước này chưa đổi hành vi, làm và kiểm riêng được.
4. Popup: thêm `PopupId`, `IPopupContent<T>`, `PopupCatalog` và interface đọc catalog; tham gia `ISessionScene` để nạp và nhả asset; đổi `IPopupService`; bỏ `PopupRequest`; bật `noEngineReferences: true`; xoá ngoại lệ ASM-05 trong `rules.json`. Sửa test của Popup. Đồng thời thêm trường catalog vào `Boot` và catalog cho variant Game (Loading chưa có popup nào).
5. Shop: thêm `Scripts/Contracts` với `ShopPopups`; chuyển `PurchasedPopup.prefab` sang `FeatureAssets/Public`; `ShopLogic` gọi `Show(ShopPopups.Purchased)` và không còn giữ `GameObject`; bỏ prefab popup khỏi `ShopConfig`.
6. Cập nhật `.claude/rules/unity.md`, `CLAUDE.md` của Popup và Shop.

Bước 4 và 5 phải đi cùng một lần biên dịch, vì đổi `IPopupService` làm Shop hỏng biên dịch.

## 8. Giá phải trả

- Mỗi asset giao cho dịch vụ thêm một bước: khai báo id và thêm vào catalog.
- Mỗi popup mới phải sửa catalog trong variant Boot của scene dùng nó (`_Composition`), việc phải hỏi người. Popup dùng ở nhiều scene thì có mặt trong nhiều catalog.
- Gõ sai id chỉ lộ lúc chạy hoặc lúc test, không lộ lúc biên dịch.
- Đổi tên feature là đổi mọi id của nó. Tên feature vốn đã không nên đổi.

Bù lại: Contracts sạch, không còn ngoại lệ ASM-05, `ShopLogic` thành C# thuần thật sự (không còn giữ `GameObject`), và có một chỗ nhìn thấy toàn bộ asset dùng chung của game, có ích khi preload hay chuyển sang Addressables.
