---
based-on: fe94740dab29
---
# Thẻ hero

Mỗi hero trong danh sách hiện thành một thẻ cho thấy hero là ai và đánh kiểu gì, để người chơi so sánh trước khi chọn.

## Ý cốt lõi

### card-content | AI 7 | Thẻ hero có hình, tên và cách đánh của hero
Cách đánh là vai trò của hero (ví dụ cận chiến chắn quái, bắn xa đứng sau) lấy từ `feature-hero/hero-roles`. Đã loại: bảng chỉ số chi tiết (nặng với người chơi phổ thông).

### card-role-mark | AI 6 | Hero cùng cách đánh có cùng một dấu hiệu nhận ra ngay
Liếc là biết đội đang nhiều chắn hay nhiều bắn xa. Dấu hiệu là biểu tượng hoặc màu theo vai trò.

### card-selected | AI 7 | Thẻ của hero đã vào đội trông khác hẳn thẻ chưa chọn

### card-read-only | AI 7 | Nội dung thẻ đọc từ `feature-hero`; màn chọn đội không giữ thông tin hero
Thêm hero mới ở `feature-hero` thì tự có thẻ mới.
