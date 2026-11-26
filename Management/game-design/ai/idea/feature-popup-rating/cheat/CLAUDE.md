# Cheat: popup mời đánh giá
Dữ liệu cốt lõi: [data.md](data.md)

## Dùng làm gì
- Phiên code dựng ngay popup mời để test và Play, không phải chơi tới lúc phá kỉ lục.
- Chỗ duy nhất đặt ảnh art của feature.

## State
- `invite-open`: một state đủ mọi phần tử của popup. Popup chỉ có một dáng, nên không cần state thứ hai.

## Luật
- Chỉ chạy ở bản dev, code nằm trong thư mục của feature, sau cờ dev.
- Bỏ qua điều kiện lúc mời và giới hạn một lần mỗi lần mở game; không lưu gì.
- Frame `invite-open.frame` do @UIDesigner vẽ, `use` số từ frame của `feature-popup-rating/rating-invite/invite-spec`.
