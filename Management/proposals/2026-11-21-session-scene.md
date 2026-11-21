# Đề xuất: dịch vụ SessionScene

Ngày: 2026-11-21. Trạng thái: chờ duyệt. Chưa có dòng code nào được sửa.

## Vì sao cần

- Feature có thể phải làm việc bất đồng bộ (đọc save, tải asset) rồi mới đủ điều kiện chơi. `Install` hiện đồng bộ và là pha duy nhất.
- View đang lấy logic qua static (`WalletRuntime`, `ShopRuntime`). Nếu view chỉ được tạo ở pha Running, bên tạo đưa logic vào luôn và static bỏ được.
- Chưa có chỗ nào để feature gỡ sự kiện, huỷ view, lưu file khi rời scene.

## Vòng đời

| Trạng thái | Việc xảy ra | Rời khỏi khi |
|---|---|---|
| Init | `Install` đồng bộ: tạo logic, đăng ký contract, feature xin tham gia phiên | mọi installer chạy xong |
| Loading | mọi feature tải bất đồng bộ, chạy song song | tất cả xong → Running; một cái lỗi → Failed |
| Running | từng feature mở view theo thứ tự cài; người chơi thao tác được | có yêu cầu kết thúc phiên |
| Disposing | từng feature gỡ sự kiện, huỷ view, lưu file, theo thứ tự ngược | tất cả xong → Disposed |
| Disposed | kết thúc | |
| Failed | một feature tải lỗi; các lượt tải còn lại bị huỷ, không feature nào mở view | xem câu hỏi 1 |

## Đặt ở đâu

Dịch vụ mới `UnityProject/Assets/Features/_Services/SessionScene/`, cùng kiểu với Popup. Không sửa `Core`.

`Boot` không điều khiển được vòng đời: validator chỉ cho `_Composition` tham chiếu `Game.Core` và assembly Internal (để thấy installer), không cho tham chiếu Contracts của dịch vụ. Vì vậy dịch vụ tự chạy:

- Installer của SessionScene tạo một object điều khiển từ prefab riêng (`FeatureAssets/Private`), qua một interface trong Internal.
- `Start()` của object đó chạy sau khi `Boot.Awake` đã cài xong mọi installer, và là lúc chuyển Init → Loading.
- Logic trạng thái nằm trong lớp C# thuần `SessionSceneLogic`; object điều khiển chỉ báo "bắt đầu" và "kết thúc".

## Contract

```csharp
namespace Game.Services.SessionScene
{
    public enum SessionState { Init, Loading, Running, Disposing, Disposed, Failed }

    public interface ISessionScene
    {
        SessionState State { get; }

        /// Chỉ gọi được trong Init (tức trong Install). Gọi muộn hơn thì ném lỗi.
        void Join(ISessionParticipant participant);

        /// Yêu cầu kết thúc phiên; xong khi đã tới Disposed.
        UniTask EndAsync();
    }

    public interface ISessionParticipant
    {
        UniTask LoadAsync(CancellationToken cancellation);
        void Run();
        UniTask DisposeAsync();
    }
}
```

Feature không cần pha nào thì trả `UniTask.CompletedTask` hoặc để trống.

## Feature dùng thế nào (ví dụ Wallet)

- `Install`: tạo `WalletLogic`, đăng ký `IWalletService`, rồi `registry.Resolve<ISessionScene>().Join(...)`.
- `LoadAsync`: hiện chưa có gì để tải.
- `Run`: tạo label số xu từ prefab và gắn presenter. View chỉ hiện thực `ICoinsDisplay.ShowCoins(int)`, không biết `WalletLogic`.
- `DisposeAsync`: gỡ `CoinsChanged`, huỷ label.
- Xoá `WalletRuntime`.

## Những chỗ cần người làm hoặc duyệt

| Việc | Vì sao AI không tự làm được |
|---|---|
| Thêm UniTask vào `UnityProject/Packages/manifest.json`, ghim một phiên bản cụ thể | `Packages` phải hỏi từng lần |
| Thêm UniTask vào `allowedPackages` trong `rules.json` cho Contracts, Internal, Tests | chỉ người sửa `rules.json` |
| Tạo thư mục `Features/_Services/SessionScene/` | guard chỉ nhận `FEATURE` trỏ tới thư mục đã có |
| Thêm installer của SessionScene lên đầu danh sách trong `Boot.prefab`; sửa số 3 thành 4 trong `BootTests` | `_Composition` phải hỏi |
| Thêm tham chiếu SessionScene Contracts và UniTask vào asmdef Internal của Wallet, Shop | sửa `*.asmdef` phải hỏi |
| Có thể cần ngoại lệ ASM-05 cho Contracts của SessionScene | UniTask phụ thuộc UnityEngine; chưa thử được Contracts có biên dịch với `noEngineReferences: true` hay không |

## Rủi ro đã biết

- **Thoát game đột ngột**: Unity không chờ việc bất đồng bộ khi thoát. `DisposeAsync` chỉ chạy trọn khi có bên gọi `EndAsync()` trước lúc rời scene. Thoát thẳng thì việc lưu file có thể không kịp.
- **Test EditMode của Boot**: `BootTests` gọi installer thật, nên installer của SessionScene sẽ tạo object điều khiển ngay trong EditMode. Cần chặn việc tạo object khi không ở Play, hoặc test phải dọn.
- **Popup vẫn cần static**: `IPopupService.Show` chỉ trả `PopupHandle`, feature gọi không đưa được logic vào popup vừa tạo. `ShopRuntime` chỉ bỏ được khi contract của Popup đổi; việc đó nằm ngoài đề xuất này.

## Thứ tự làm đề nghị

1. Người thêm UniTask và sửa `rules.json`, tạo thư mục dịch vụ.
2. Dựng SessionScene kèm test EditMode bằng feature giả (phiên `FEATURE=_Services/SessionScene`).
3. Chuyển Wallet sang dùng, xoá `WalletRuntime` (phiên `FEATURE=Wallet`).
4. Thêm installer vào `Boot.prefab`, sửa `BootTests`.
5. Shop: làm sau khi chốt hướng cho Popup.

## Câu hỏi cần người quyết

1. **Từ Failed đi đâu?** Đề nghị: vẫn gọi `DisposeAsync` cho mọi feature đã tham gia rồi dừng ở Failed, để không rò sự kiện hay file đang mở. Bảng gốc chưa ghi.
2. **Loading song song hay tuần tự?** Đề nghị song song, vì các feature độc lập. Nếu sau này có feature phải tải sau feature khác thì cần bàn lại.
3. **Ai gọi `EndAsync()`?** Hiện chưa có feature chuyển scene. Đề nghị để sẵn trong contract, chưa có bên gọi cho tới khi có feature đó.
4. **Một feature tải quá lâu?** Đề nghị chưa đặt giới hạn thời gian ở bản đầu.
