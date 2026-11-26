# Popup (dịch vụ)
Tầng: 5 · Services

Xếp chồng popup của scene (`live/feature-popup`): tối đa 3, mỗi id một bản, popup mới nằm trên và là popup duy nhất nhận bấm.
Không sở hữu popup nào. Feature gọi xin mở bằng id; prefab, nội dung và nút là của feature gọi.

- Contract: `IPopupService` (`Show(PopupId)`, `Close`), `IPopupViews` (`TryGetView<T>`, `IsOnTop`), `IPopupStack` (`OpenIds`: id đang mở, đáy trước, bản chụp), `PopupId`, `PopupHandle`. Contracts thuần C#.
- Mở id đang mở (cảnh báo), khi đã đủ 3, id không có trong danh sách của scene, hoặc khi phiên chưa Running (lỗi nêu id): từ chối, trả handle không hợp lệ. Id mặc định là lỗi lập trình: ném `ArgumentException`.
- `Close` đóng được popup ở mọi vị trí; handle đã đóng hay lạ thì bỏ qua.
- Prefab theo id lấy từ catalog loại "Popup" của scene (`AssetCatalog` của Core, trên variant Boot trong `_Composition`).
- Tham gia phiên (`PopupParticipant`): tải thì kiểm mọi mục catalog (lỗi nêu id); `Run` mới cho mở; huỷ thì bỏ mọi popup, không báo gì thêm. Cài sau SessionScene.
- Lớp popup: `PopupLayer.prefab` (canvas overlay, sorting 1000), mỗi popup một `PopupBackdrop.prefab` (đen 60%, chặn chạm) ngay sau nó. Số tối đa trong `PopupConfig.asset`. Tất cả ở `FeatureAssets/Private`.
- Nút quay lại của điện thoại: feature gọi tự đọc và chỉ xử lí khi `IsOnTop(handle)`. Dịch vụ không tự đóng popup.
- Logic chồng trong `PopupService`; tạo/huỷ object qua `IPopupPresenter`, ghi log qua `IPopupLog`.
- Không biết feature nào. Cấm code trường hợp riêng cho một feature.
- Cheat `sample-popup` (`feature-popup/cheat`, `Scripts/Internal/Cheat`): chọn `cheat = SamplePopup` trên `PopupInstaller.asset` (mặc định `None`). Ở bản dev, `SamplePopupCheat` (Join sau `PopupParticipant`) mở popup mẫu `_Services/Popup/DevSample` (thiết kế ghi `Popup/DevSample`; SRC-05 bắt id của dịch vụ mang tên thư mục) qua đúng `Show`/`Close`; nút "Đóng" (`DevSampleView` ở View) đóng như mọi bên gọi. Prefab `PopupDevSample.prefab` gắn vào `PopupConfig.devSamplePrefab`; field luôn serialize, code đọc nó và id nằm sau `#if UNITY_EDITOR || DEVELOPMENT_BUILD`. Nợ: asset nhỏ vẫn lọt vào bản phát hành, bước build loại nó để tới PROTOTYPE (@CodeLead chốt). Không mở id của feature khác, không đụng trận. Cảnh popup đè trận là cheat của feature sở hữu popup.
- Prefab popup của feature gọi phải là UI (RectTransform): nó được đặt vào canvas của lớp popup.

Test: assembly `Game.Services.Popup.Tests` (EditMode).
