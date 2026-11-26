---
based-on: c2f8ded28909
refs: feature-upgrade/upgrade-purchase/purchase-rules@ba02d7fd60f1, feature-hud/hud-layout/layout-spec@aa79f655e44b
---
# Hiển thị và chạm nút nâng cấp

Mỗi nút hiện 3 thứ: hình của mục, cấp hiện tại, giá cấp sau; có 2 trạng thái sáng và mờ. Một chạm vào nút sáng gửi đúng một yêu cầu mua.

## Ý cốt lõi

### three-things | AI 8 | Mỗi nút hiện đúng 3 thứ: hình của mục, chữ "Cấp N" và giá cấp sau kèm biểu tượng tiền
Ví dụ nút hero 1 vừa vào trận: chân dung hero đó, "Cấp 1", giá 30.

### icons | AI 8 | Có 4 loại hình: chân dung từng hero, biểu tượng máu thành, biểu tượng đòn tự bắn, biểu tượng kĩ năng thành
Chân dung hero là hình của đúng hero ở chỗ đó. Biểu tượng kĩ năng đại diện cho cả ba kĩ năng, không phải một kĩ năng riêng.

### bright-dim | AI 8 | Nút sáng khi mua được và mờ khi không mua được, đổi trong cùng 1 khung hình với lúc số dư hoặc cấp đổi
"Mua được" lấy từ `feature-upgrade`; nút không tự so tiền với giá.

### tap-once | AI 8 | 1 chạm vào nút sáng gửi đúng 1 yêu cầu mua; chạm nút mờ gửi 0 yêu cầu
Không xác nhận. Chạm liên tiếp nhanh thì mỗi chạm một yêu cầu; `feature-upgrade` từ chối yêu cầu không đủ tiền.

### buy-feedback | AI 7 | Mua thành công thì nút nảy lên một lần trong 0,2 giây và cấp, giá trên nút đổi ngay
Người chơi biết chạm đã có tác dụng. Mua bị từ chối thì không nảy.

### short-number | AI 7 | Giá từ 10.000 tiền trở lên hiện rút gọn: 10.000-999.999 thành "12,3K", từ 1.000.000 thành "1,2M"
Rút gọn cắt bớt, giữ một chữ số thập phân (12.399 thành "12,3K"). Dưới 10.000 hiện đủ số. Sáng hay mờ vẫn theo giá thật.
