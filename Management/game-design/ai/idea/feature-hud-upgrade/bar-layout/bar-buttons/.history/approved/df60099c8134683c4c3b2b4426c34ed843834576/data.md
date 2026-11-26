---
based-on: 4a9d06c74198
refs: feature-upgrade/upgrade-list/item-set@fd6c29c2b1b5, feature-hud-upgrade/button-state/button-display@a56d1f8d5995, feature-hud/hud-layout/layout-spec@3fb86a404186
---
# Nút trên thanh nâng cấp

Thanh nâng cấp có đúng 6 nút chia hai nhóm 3 nút: nhóm hero và nhóm thành. Thanh hiện từ lúc trận bắt đầu tới lúc lượt kết thúc.

## Ý cốt lõi

### six-buttons | AI 8 | Thanh có đúng 6 nút, mỗi nút ứng với một trong 6 mục nâng cấp
Không có nút nào khác trên thanh (không nút đóng, không nút mở rộng).

### hero-group | AI 8 | Nhóm hero có 3 nút theo thứ tự chỗ 1, 2, 3 của đội
Nút hero chỗ 1 nằm ở đầu nhóm.

### castle-group | AI 8 | Nhóm thành có 3 nút theo thứ tự máu, sức đánh, kĩ năng
Máu: máu thành. Sức đánh: đòn tự bắn của thành. Kĩ năng: cả ba kĩ năng thành.

### group-gap | AI 7 | Giữa 2 nhóm có một khoảng trống rộng hơn khoảng cách giữa hai nút trong cùng nhóm
Liếc là thấy hai nhóm; kiểm bằng ảnh chụp màn hình. Độ rộng cụ thể do người làm giao diện quyết.

### show-when | AI 8 | Thanh hiện từ khung hình đầu của trận tới khi popup kết thúc lượt hiện, kể cả lúc nghỉ giữa đợt, 0 lần ẩn giữa chừng


### no-cover | AI 7 | Thanh không che dải đất chiến trường: 0 phần của dải đất nằm dưới thanh
Kiểm bằng ảnh chụp màn hình ở tỉ lệ 16:9 và 20:9.
