---
based-on: 39e910418294
refs: feature-run/run-flow@45cc90c1501f, feature-run/run-score@691534d6dde7, feature-scene-flow/scene-list@78ed2dcc0859
---
# Nội dung màn bắt đầu

Màn bắt đầu gồm ba thứ: tên game, kỉ lục hiện tại và nút chơi. Không có gì khác để bấm ngoài nút chơi.

## Ý cốt lõi

### home-items | AI 7 | Màn bắt đầu chỉ có tên game, kỉ lục và một nút chơi
Ít thứ để người chơi phổ thông vào trận ngay. Đã loại: cài đặt, cửa hàng, danh sách màn (vòng 1 không có).

### home-play | AI 8 | Bấm nút chơi là sang màn chọn đội
Feature này chỉ báo "đã bấm chơi"; chuyển màn do `feature-run/run-flow`.

### home-record-source | AI 7 | Kỉ lục hiện ở màn bắt đầu lấy từ `feature-run/run-score`, chỉ đọc
Kỉ lục mới vừa đạt ở lượt trước hiện ngay khi quay về màn này. Feature này không tính hay lưu kỉ lục.

### home-entry | AI 7 | Người chơi tới màn bắt đầu theo hai đường: mở game, hoặc chọn về màn bắt đầu ở popup thua
