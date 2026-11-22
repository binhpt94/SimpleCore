# Luồng màn hình của lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lượt gồm đúng một lần đi qua màn chọn đội và đúng một trận.
- Một trận kết thúc theo một trong hai cách: thành bị phá (thua) hoặc người chơi bỏ lượt.
- Feature này là bên duy nhất quyết định sang màn nào; các màn khác chỉ báo người chơi đã bấm gì.
- `feature-castle` báo thành bị phá; `feature-wave`, `feature-hero`, `feature-enemy`, `feature-castle` nhận lệnh dừng khi lượt kết thúc.
- `feature-home`, `feature-team-select`, `feature-popup-lose`, `feature-popup-pause` báo nút đã bấm; feature này chuyển màn.
- `feature-scene-flow` làm việc chuyển scene khi feature này xin.

## Định nghĩa
- **Lượt**: từ lúc vào trận tới lúc trận kết thúc. Màn bắt đầu và màn chọn đội nằm ngoài lượt.
- **Trận**: phần chơi trên dải chiến trường, có HUD (`feature-hud`) và thanh nâng cấp (`feature-hud-upgrade`).
- **Kết thúc lượt**: lúc trận dừng hẳn và điểm của lượt được chốt (`feature-run/run-score`).
- **Scene menu**: chứa màn bắt đầu và màn chọn đội. **Scene trận**: chứa trận, popup tạm dừng, popup thua.

## Danh sách
Các bước chuyển:
| Từ | Người chơi làm | Tới |
|---|---|---|
| Mở game (sau màn tải của `feature-scene-flow`) | | Màn bắt đầu |
| Màn bắt đầu | Bấm chơi | Màn chọn đội |
| Màn chọn đội | Bấm vào trận (đội đủ chỗ) | Trận |
| Trận | Thành bị phá | Popup thua |
| Trận | Bấm tạm dừng | Popup tạm dừng |
| Popup tạm dừng | Tiếp tục | Trận |
| Popup tạm dừng | Bỏ lượt | Màn bắt đầu |
| Popup thua | Chơi lại | Màn chọn đội |
| Popup thua | Về màn bắt đầu | Màn bắt đầu |

## Luật cơ bản
- Vào trận thì bắt đầu lượt mới (`feature-run/run-start`).
- Thành bị phá thì kết thúc lượt, rồi hiện popup thua.
- Bỏ lượt thì kết thúc lượt, rồi về màn bắt đầu.
- Đã kết thúc lượt thì trận không chạy tiếp, kể cả khi popup thua đang mở.
- Không có hồi sinh khi thua: từ popup thua chỉ có chơi lại hoặc về màn bắt đầu.

## Đã tự quyết
- **Hai scene chơi: menu và trận.** Tải mới scene trận mỗi lượt khớp `feature-session-scene/session-clean` (rời scene là dọn sạch). Màn bắt đầu và chọn đội nhẹ, để chung cho chuyển nhanh. Đã loại: một scene cho tất cả; mỗi màn một scene.
- **Bỏ lượt về thẳng màn bắt đầu, không hiện popup thua.** Theo `feature-popup-pause/pause-actions`. Điểm vẫn chốt như thua (quyết định chung của game).
- **Chơi lại về màn chọn đội.** Theo `feature-run/run-replay`.

## Chưa rõ
- Cách chia scene (menu, trận) cần `feature-scene-flow` thống nhất; nếu bên đó chọn khác thì sửa ý `flow-scenes`.
