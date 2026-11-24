---
based-on: e8c7b20b31fd
refs: feature-run/run-flow/flow-rules@9109afcd6a93, feature-run/run-score/score-rules@54a8417a7ccd, feature-hud/hud-layout/layout-spec@ebefd7f8b116
---
# Thành phần màn bắt đầu

Màn bắt đầu có đúng 3 thành phần: dòng tên game, dòng kỉ lục và 1 nút "Chơi". Ghi rõ chữ hiện ra và cách nút phản ứng.

## Ý cốt lõi

### home-title-text | AI 7 | Dòng tên game hiện chữ "Thủ thành anh hùng", không bấm được
Tên tạm của game (node gốc). Đổi tên chỉ đổi chữ.

### home-record-text | AI 7 | Dòng kỉ lục hiện "Kỉ lục: N đợt", N là kỉ lục hiện tại, chưa có thì N = 0
Đọc lại mỗi lần màn hiện, nên vừa phá kỉ lục rồi về màn này là thấy số mới.

### home-play-button | AI 8 | Có đúng 1 nút "Chơi"; chạm 1 lần là báo đã bấm chơi, sang màn chọn đội
Chỉ lần chạm đầu có tác dụng; chạm thêm trong lúc đổi màn bị bỏ qua.

### home-nothing-else | AI 7 | Ngoài nút "Chơi", không có chỗ nào trên màn bấm được
Không nút thoát, không cài đặt, không cửa hàng.
