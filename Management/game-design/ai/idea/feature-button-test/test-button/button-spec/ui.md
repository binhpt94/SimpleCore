## Phần tử
- test-button | nút | chính | bấm thử, ghi "Thử" | trạng thái: bật (luôn bật khi màn trận hiện)
- test-counter | nhãn | chính | cho thấy số lần đã bấm, ghi "Đã bấm: N" | trạng thái: một trạng thái, chữ đổi theo N

## Tương tác
- test-button chạm rồi nhả trên nút: N tăng 1, test-counter đổi chữ ngay.
- test-button chạm rồi kéo ra ngoài mới nhả: không làm gì.
- test-button giữ: không có thao tác riêng, nhả trên nút thì tính như chạm.
- test-counter: không tương tác.

## Flow
- Màn trận mở → test-button và test-counter hiện, "Đã bấm: 0".
- Chạm test-button → "Đã bấm: 1", "Đã bấm: 2"…
- Popup bất kì mở (tạm dừng, hồi sinh, thua) → popup che lên, chạm không tới test-button; đóng popup → bấm được lại, N giữ nguyên.
- Rời màn trận → cả hai ẩn, N bỏ đi; lần sau màn trận mở lại từ "Đã bấm: 0".
