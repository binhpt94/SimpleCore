# Luồng màn hình của lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lượt gồm đúng một lần đi qua màn chọn đội và đúng một trận.
- Một trận kết thúc theo một trong hai cách: thành bị phá (thua) hoặc người chơi bỏ lượt.
- Ở IDEA (chỉ có màn trận) feature này tự đổi màn. Từ PROTOTYPE, `feature-game-flow` là bên duy nhất chọn màn chính (bắt đầu, chọn đội, trận); feature này lo các bước trong trận (tạm dừng, hồi sinh, popup thua), dọn trận rồi chuyển lựa chọn rời trận cho điều hướng màn.
- `feature-castle` báo thành bị phá; `feature-wave`, `feature-hero`, `feature-enemy`, `feature-castle` nhận lệnh dừng khi lượt kết thúc.
- `feature-popup-lose`, `feature-popup-pause` báo nút đã bấm cho feature này. Từ PROTOTYPE `feature-home`, `feature-team-select` báo thẳng cho `feature-game-flow`.
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
| Trận | Thành bị phá lần đầu trong lượt | Popup hồi sinh |
| Popup hồi sinh | Xem xong quảng cáo | Trận (chạy tiếp) |
| Popup hồi sinh | Bỏ qua, hoặc quảng cáo không xem được | Popup thua |
| Trận | Thành bị phá lần hai | Popup thua |
| Trận | Bấm tạm dừng | Popup tạm dừng |
| Popup tạm dừng | Tiếp tục | Trận |
| Popup tạm dừng | Bỏ lượt | Màn bắt đầu |
| Popup thua | Chơi lại | Màn chọn đội |
| Popup thua | Về màn bắt đầu | Màn bắt đầu |

## Luật cơ bản
- Vào trận thì bắt đầu lượt mới (`feature-run/run-start`).
- Thành bị phá thì trận dừng; lần đầu trong lượt thì đề nghị hồi sinh, không hồi sinh thì kết thúc lượt rồi hiện popup thua.
- Bỏ lượt thì kết thúc lượt, rồi về màn bắt đầu.
- Đã kết thúc lượt thì trận không chạy tiếp, kể cả khi popup thua đang mở.
- Hồi sinh chỉ có ở popup hồi sinh (`feature-popup-revive`), mỗi lượt một lần; từ popup thua chỉ có chơi lại hoặc về màn bắt đầu.

## Đã tự quyết (thêm, 2026-11-25)
- **Từ PROTOTYPE, bảng đường đi giữa các màn chính chuyển sang `feature-game-flow`** (nhánh đường đi của nó). Đã sửa mục Quan hệ (2026-11-26, khi vào PROTOTYPE). Câu "bên duy nhất quyết định" chỉ nằm ở mục Quan hệ, không nằm trong ý nào, nên hash không đổi; việc của phiên Run ghi ở mục Làm gì và bảng giai đoạn của node vòng 4 `flow-rules`.

## Đã tự quyết (thêm, 2026-11-24)
- **Bước hồi sinh đứng trước popup thua** (ý `flow-revive-step`, người yêu cầu thêm hồi sinh bằng quảng cáo). Thay luật cũ "không có hồi sinh khi thua".

## Đã tự quyết
- **Mọi màn trong scene Game, theo `feature-scene-flow/scene-list` (2026-11-23).** Viết lại ý `flow-scenes`: trước ghi "scene menu và scene trận", lệch với scene-list là nơi quyết scene. Lượt mới vẫn sạch vì trận dựng mới, huỷ hết mỗi lần (`feature-run/run-flow/flow-rules`, ý `fresh-battle`). Đã loại: scene menu và scene trận riêng; mỗi màn một scene.
- **Bỏ lượt về thẳng màn bắt đầu, không hiện popup thua.** Theo ý pause-actions của `feature-popup-pause`. Điểm vẫn chốt như thua (quyết định chung của game).
- **Chơi lại về màn chọn đội.** Theo ý run-replay của `feature-run`.

## Chưa rõ
- Không có điểm chặn.
