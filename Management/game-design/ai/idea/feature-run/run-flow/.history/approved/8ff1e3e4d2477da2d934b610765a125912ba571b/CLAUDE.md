# Luồng màn hình của lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lượt gồm đúng một lần đi qua màn chọn đội và đúng một trận.
- Một trận kết thúc khi thành bị phá (thua).
- Ở IDEA (chỉ có màn trận) feature này tự đổi màn. Từ PROTOTYPE, `feature-game-flow` là bên duy nhất chọn màn chính (bắt đầu, chọn đội, trận); feature này lo các bước trong trận (popup thua), dọn trận rồi chuyển lựa chọn rời trận cho điều hướng màn.
- `feature-castle` báo thành bị phá; `feature-wave`, `feature-hero`, `feature-enemy`, `feature-castle` nhận lệnh dừng khi lượt kết thúc.
- `feature-popup-lose` báo nút đã bấm cho feature này. Từ PROTOTYPE `feature-home`, `feature-team-select` báo thẳng cho `feature-game-flow`.
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
| Popup thua | Chơi lại | Màn chọn đội |
| Popup thua | Về màn bắt đầu | Màn bắt đầu |

## Luật cơ bản
- Vào trận thì bắt đầu lượt mới (`feature-run/run-start`).
- Thành bị phá thì trận dừng, kết thúc lượt rồi hiện popup thua.
- Đã kết thúc lượt thì trận không chạy tiếp, kể cả khi popup thua đang mở.
- Từ popup thua chỉ có chơi lại hoặc về màn bắt đầu.

## Đã tự quyết (thêm, 2026-11-25)
- **Từ PROTOTYPE, bảng đường đi giữa các màn chính chuyển sang `feature-game-flow`** (nhánh đường đi của nó). Đã sửa mục Quan hệ (2026-11-26, khi vào PROTOTYPE). Câu "bên duy nhất quyết định" chỉ nằm ở mục Quan hệ, không nằm trong ý nào, nên hash không đổi; việc của phiên Run ghi ở mục Làm gì và bảng giai đoạn của node vòng 4 `flow-rules`.

## Đã tự quyết
- **Mọi màn trong scene Game, theo `feature-scene-flow/scene-list` (2026-11-23).** Viết lại ý `flow-scenes`: trước ghi "scene menu và scene trận", lệch với scene-list là nơi quyết scene. Lượt mới vẫn sạch vì trận dựng mới, huỷ hết mỗi lần (`feature-run/run-flow/flow-rules`, ý `fresh-battle`). Đã loại: scene menu và scene trận riêng; mỗi màn một scene.
- **Chơi lại về màn chọn đội.** Theo ý run-replay của `feature-run`.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ popup hồi sinh (ý `flow-revive-step`), popup tạm dừng và bỏ lượt khỏi các bước chuyển; thành bị phá là vào popup thua.

## Chưa rõ
- Không có điểm chặn.
