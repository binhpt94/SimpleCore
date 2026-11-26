---
based-on: 72f1cb81f16b
refs: feature-upgrade/upgrade-purchase@dfecac7ac2de, feature-upgrade/upgrade-list@7294a54c79df, feature-hud-upgrade/bar-layout@4a9d06c74198
---
# Trạng thái nút nâng cấp

Mỗi nút cho thấy nó nâng cái gì, đang ở cấp mấy, cấp sau giá bao nhiêu, và mờ khi không đủ tiền. Một chạm là gửi yêu cầu mua một cấp.

## Ý cốt lõi

### button-shows | AI 8 | Mỗi nút hiện hình của mục, cấp hiện tại và giá cấp sau
Hình là chân dung hero hoặc biểu tượng máu, sức đánh, kĩ năng của thành. Đã loại: chỉ hiện giá (không biết đã nâng tới đâu).

### button-dim | AI 8 | Nút mờ khi không mua được, sáng lại ngay khi mua được
Không mua được là không đủ tiền. Cập nhật mỗi khi số dư hoặc cấp đổi. Đã loại: ẩn nút không đủ tiền (thanh nhảy chỗ).

### button-one-tap | AI 8 | Chạm một lần vào nút sáng là mua một cấp
Không có bước xác nhận. Chạm nút mờ thì không mua. Đã loại: giữ để mua, hỏi lại trước khi mua.

### button-no-logic | AI 7 | Nút không tự tính giá hay quyết mua; mọi thứ lấy từ feature-upgrade
Nút chỉ hiện thông tin được đưa và chuyển cú chạm xuống `feature-upgrade/upgrade-purchase`. Đã loại: nút tự so tiền với giá.
