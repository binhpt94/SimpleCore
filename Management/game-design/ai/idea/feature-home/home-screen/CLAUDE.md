# Nội dung màn bắt đầu

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một màn bắt đầu cho cả game; nằm trong scene Game cùng mọi màn khác (`feature-scene-flow/scene-list`), là màn đầu tiên khi vào scene Game.
- Đọc kỉ lục từ `feature-run/run-score`.
- Báo bấm chơi cho `feature-run/run-flow`; màn tiếp theo là `feature-team-select`.
- Tới từ: màn tải của `feature-scene-flow` khi mở game, bỏ lượt ở `feature-popup-pause`, nút về màn bắt đầu ở `feature-popup-lose`.

## Định nghĩa
- `glossary#record`

## Danh sách
Thứ hiện trên màn bắt đầu:
- Tên game.
- Kỉ lục hiện tại.
- Nút chơi.

## Luật cơ bản
- Mỗi lần màn bắt đầu hiện, kỉ lục là giá trị mới nhất.
- Bấm nút chơi: sang màn chọn đội.
- Màn này không giữ trạng thái gì của lượt.

## Đã tự quyết
- **Ghép bốn ý thành một node.** Feature nhỏ, hai ý vòng 2 (`home-start`, `home-record`) đều nằm trên một màn.
- **Màn bắt đầu ở scene Game (2026-11-23, sửa lệch).** Bản cũ ghi "scene menu", trái với `feature-scene-flow/scene-list`. Đã loại: scene menu riêng.
- **Không có nút thoát game.** Điện thoại tự có cách thoát. Đã loại: nút thoát.

## Chưa rõ
- Tên game chính thức chưa có (node gốc): hiện tên tạm.
- Chưa chơi lượt nào thì kỉ lục hiện ra sao: vòng 4.
