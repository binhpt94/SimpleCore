# Popup (dịch vụ)

Quản lý hàng đợi popup: mỗi lúc chỉ một popup mở, cái sau chờ cái trước đóng.
Không sở hữu popup nào. Feature gọi tự mang prefab của mình tới qua `PopupRequest`.

- Contract: `IPopupService` (`Show`, `Close`), `PopupRequest`, `PopupHandle`.
- Không biết feature nào: không tham chiếu Contracts hay asset của feature khác.
- Cấm code trường hợp riêng cho một feature cụ thể.
- `Contracts` ở đây được dùng UnityEngine (nhận `GameObject` prefab): ngoại lệ ASM-05 đã duyệt trong `rules.json`.
- Logic hàng đợi nằm trong `PopupService`, không đụng Unity; việc tạo/huỷ object đi qua `IPopupPresenter`.
- Chưa có lớp (layer) hiển thị: chưa feature nào cần.

Test: assembly `Game.Services.Popup.Tests` (EditMode).
