# Popup (dịch vụ)

Quản lý hàng đợi popup: mỗi lúc chỉ một popup mở, cái sau chờ cái trước đóng.
Không sở hữu popup nào. Feature gọi xin mở popup bằng id; prefab là của feature gọi.

- Contract: `IPopupService` (`Show(PopupId)`, `Close`), `PopupId`, `PopupHandle`. Contracts thuần C#.
- `PopupId` dạng `<Owner>/<Name>` (ví dụ `Shop/Purchased`); constructor ném lỗi nếu sai dạng.
- Prefab theo id lấy từ catalog loại "Popup" của scene (`AssetCatalog` của Core, nằm trên variant Boot của scene trong `_Composition`).
- Installer lỗi ngay nếu scene không có catalog "Popup" hoặc catalog có mục không phải popup id hay không phải prefab.
- `Show` với id không có trong catalog: lỗi ngay, nêu id và tên catalog. Không âm thầm bỏ qua.
- Không biết feature nào: không tham chiếu Contracts hay asset của feature khác. Cấm code trường hợp riêng cho một feature.
- Logic hàng đợi nằm trong `PopupService`; tra prefab qua `IPopupPrefabs`, tạo/huỷ object qua `IPopupPresenter`.
- Chưa nạp/nhả asset qua SessionScene: catalog trỏ thẳng prefab nên prefab nạp cùng scene. Làm khi chuyển sang Addressables.

Test: assembly `Game.Services.Popup.Tests` (EditMode).
