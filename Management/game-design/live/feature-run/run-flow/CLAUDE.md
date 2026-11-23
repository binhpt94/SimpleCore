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
- `glossary#run`

## Danh sách
Các bước chuyển:
| Từ | Người chơi làm | Tới |
|---|---|---|
| Mở game (scene Loading tải xong, sang scene Game) | | Màn bắt đầu |
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
- **Mọi màn trong scene Game, theo `feature-scene-flow/scene-list` (2026-11-23).** Viết lại ý `flow-scenes`: trước ghi "scene menu và scene trận", lệch với scene-list là nơi quyết scene. Lượt mới vẫn sạch vì trận dựng mới, huỷ hết mỗi lần (`feature-run/run-flow/flow-rules`, ý `fresh-battle`). Đã loại: scene menu và scene trận riêng; mỗi màn một scene.
- **Bỏ lượt về thẳng màn bắt đầu, không hiện popup thua.** Theo ý pause-actions của `feature-popup-pause`. Điểm vẫn chốt như thua (quyết định chung của game).
- **Chơi lại về màn chọn đội.** Theo ý run-replay của `feature-run`.

## Chưa rõ
- Không có điểm chặn.
