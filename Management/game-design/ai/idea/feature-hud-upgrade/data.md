---
based-on: ca84fa282d14
by: hien-doi-tuong-nang-cap=gpt@r2-1#hien-doi-tuong-nang-cap, hien-gia-nang-cap=gpt@r2-1#hien-gia-nang-cap, upgrade-bar-state=gpt@r2-1#ro-tien-do-nang-cap
---
# Thanh nâng cấp

Cụm nút trên màn hình trận để mua nâng cấp bằng một chạm mà không dừng trận, không che chiến trường.

## Ý cốt lõi

### upgrade-bar-always | AI 8 | Thanh nút nâng cấp luôn hiện trên màn hình trận, một chạm là mua
Đã loại: bảng nâng cấp bật lên (che trận, phải dừng).

### hien-doi-tuong-nang-cap | AI 7 | Mỗi nút cho biết nó nâng thành hay nâng hero nào trong đội
Giữa lúc giao tranh người chơi phải nhận ra ngay nút dành cho ai trước khi chạm.

### hien-gia-nang-cap | AI 7 | Mỗi nút cho biết giá của lần nâng cấp kế tiếp
Biết giá thì cân nhắc được mua ngay hay để tiền cho mục khác.

### upgrade-bar-state | AI 7 | Mỗi nút cho thấy mua được hay chưa, và cấp mà mục đó đã đạt trong lượt
Hiện cấp đã đạt, không phải cấp sắp mua, để người chơi thấy mình đã đầu tư vào thành và từng hero tới đâu.

### upgrade-bar-edge | AI 7 | Thanh nằm ở mép màn hình, không che dải chiến trường
