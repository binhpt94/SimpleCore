# Popup (dịch vụ)

Xếp chồng popup của scene (`live/feature-popup`): tối đa 3, mỗi id một bản, popup mới nằm trên và là popup duy nhất nhận bấm.
Không sở hữu popup nào. Feature gọi xin mở bằng id; prefab, nội dung và nút là của feature gọi.

- Contract: `IPopupService` (`Show(PopupId)`, `Close`), `IPopupViews` (`TryGetView<T>`, `IsOnTop`), `PopupId`, `PopupHandle`. Contracts thuần C#.
- Mở id đang mở (cảnh báo), khi đã đủ 3, hoặc khi phiên chưa Running (lỗi): từ chối, trả handle không hợp lệ. Id lạ hay id mặc định: ném lỗi.
- `Close` đóng được popup ở mọi vị trí; handle đã đóng hay lạ thì bỏ qua.
- Prefab theo id lấy từ catalog loại "Popup" của scene (`AssetCatalog` của Core, trên variant Boot trong `_Composition`).
- Tham gia phiên (`PopupParticipant`): tải thì kiểm mọi mục catalog (lỗi nêu id); `Run` mới cho mở; huỷ thì bỏ mọi popup, không báo gì thêm. Cài sau SessionScene.
- Lớp popup: `PopupLayer.prefab` (canvas overlay, sorting 1000), mỗi popup một `PopupBackdrop.prefab` (đen 60%, chặn chạm) ngay sau nó. Số tối đa trong `PopupConfig.asset`. Tất cả ở `FeatureAssets/Private`.
- Nút quay lại của điện thoại: feature gọi tự đọc và chỉ xử lí khi `IsOnTop(handle)`. Dịch vụ không tự đóng popup.
- Logic chồng trong `PopupService`; tạo/huỷ object qua `IPopupPresenter`, ghi log qua `IPopupLog`.
- Không biết feature nào. Cấm code trường hợp riêng cho một feature.
- Cheat `popup-over-battle` (mở `PopupPause/Main` giữa trận) do PopupPause làm, vì id là của nó. Popup không có cheat riêng.
- Prefab popup của feature gọi phải là UI (RectTransform): nó được đặt vào canvas của lớp popup.

Test: assembly `Game.Services.Popup.Tests` (EditMode).
