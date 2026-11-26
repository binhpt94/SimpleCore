---
based-on: ca84fa282d14
by: upgrade-items=gpt@r2-1#chon-muc-tieu-nang-cap, upgrade-cost=gpt@r2-2#gia-lan-mua-ke, hero-applies-level=gpt@r2-2#ap-chi-so-hero, castle-applies-level=gpt@r2-2#ap-chi-so-thanh, wallet-checks-money=gpt@r2-2#vi-xet-du-tien, upgrade-buyable=gpt@r2-2#trang-thai-mua-tung-muc, bar-reads-upgrade=gpt@r2-2#nguon-du-lieu-nut, bar-sends-item=gpt@r2-2#noi-nhan-yeu-cau-mua, buy-transaction=gpt@r2-3#giao-dich-mua, buy-needs-running-run=gpt@r2-3#trang-thai-luc-xu-ly, shared-team-slots=gpt@r2-3#chung-vi-tri-doi, wallet-notifies-balance=gpt@r2-3#bao-doi-so-du, bar-one-snapshot=gpt@r2-3#cap-nhat-sau-nut
---
# Nâng cấp

Cách người chơi biến tiền thành sức mạnh: nâng từng hero trong đội và nâng thành, bất kì lúc nào trong trận; trận không dừng và nâng cấp có hiệu lực ngay.

## Ý cốt lõi

### upgrade-anytime | AI 8 | Mua nâng cấp lúc nào cũng được, trận không dừng
Kể cả lúc đang đánh. Đã loại (người loại ở vòng 1): chỉ nâng cấp giữa hai đợt.

### buy-needs-running-run | AI 7 | Nâng cấp chỉ nhận lần mua khi `feature-run` xác nhận lượt của yêu cầu đang chạy lúc xét
Yêu cầu còn chờ bị từ chối nếu lượt đã kết thúc hay đã thay bằng lượt mới; lúc thở vẫn mua được.

### upgrade-instant | AI 8 | Mua xong là mạnh lên ngay
Kể cả với hero đang gục chờ hồi sinh.

### upgrade-items | AI 7 | Người chơi chọn nâng thành hoặc một hero cụ thể trong đội đã chọn cho lượt
Mục nâng cấp gắn với đội của lượt; người chơi quyết đầu tư vào ai. Đã loại: bốc thẻ nâng cấp ngẫu nhiên (khó nắm với người chơi phổ thông).

### shared-team-slots | AI 7 | `feature-run` cấp cùng một bảng chỗ trong đội cho `feature-hero` và nâng cấp
Chỗ trong đội là định danh của hero trong lượt; nút của chỗ 2 vẫn nâng đúng hero ở chỗ đó khi hero gục hay hồi sinh.

### upgrade-owns-levels | AI 8 | Nâng cấp giữ cấp đã mua của từng hero và của thành trong lượt
Mọi nút trên thanh đọc cấp từ cùng một nguồn. Theo câu hỏi chung vòng 2 (`_general/r2-all`, góp ý `chu-so-huu-cap-nang`).

### upgrade-cost | AI 7 | Giá lần mua kế tiếp tính theo cấp hiện tại của chính mục đó, càng nâng càng đắt
Thông số chỉnh giá do `feature-balance` giữ; nâng cấp tính ra giá cụ thể và gửi số tiền cần trừ cho ví. Giá tăng dần tự hãm, nên không cần cấp tối đa. Đã loại: giá cố định.

### buy-transaction | AI 7 | Một lần mua là trừ tiền, tăng cấp và áp hiệu lực cùng xong trong một lần xử lí
Đòn phá thành chỉ có thể xảy ra trước hoặc sau trọn lần mua, không chen giữa.

### buy-one-level | AI 7 | Mỗi lần mua trừ đúng giá của lần đó và tăng đúng một cấp, các lần chạm liền nhau xử lí lần lượt
Hai lần chạm có thể cùng thấy một giá; lần sau tính theo cấp mới. Theo câu hỏi chung vòng 2 (`mua-nang-cap-tron-ven`).

### hero-applies-level | AI 7 | `feature-hero` áp chỉ số theo cấp do nâng cấp cấp, khi hero lên cấp hoặc hồi sinh
Nâng cấp giữ cấp; hero tự cập nhật chỉ số chiến đấu. Hero mua cấp lúc đang gục thì hồi sinh với chỉ số theo cấp mới.

### castle-applies-level | AI 7 | `feature-castle` áp ngay hiệu lực của mục thành vừa lên cấp, theo cấp mới nâng cấp báo
Chỉ một bên áp hiệu lực: nâng cấp báo cấp, thành ghi thay đổi vào chỉ số của mình.

### wallet-checks-money | AI 7 | `feature-wallet` trả lời đủ tiền hay chưa cho mức giá được hỏi
Khi mua thật, ví kiểm lại bằng số dư lúc xử lí và chỉ trừ tiền nếu đủ.

### wallet-notifies-balance | AI 6 | `feature-wallet` báo số dư mới cho nâng cấp sau mỗi lần số dư đổi
Thưởng, mua hay lượt mới đều có thể đổi trạng thái nhiều nút cùng lúc.

### upgrade-buyable | AI 7 | Nâng cấp quyết mục có mua được hay chưa, từ trạng thái trận và câu trả lời của ví
Ví chỉ trả lời về tiền; nâng cấp ghép thêm điều kiện trận đang chạy và báo lại cho thanh khi giá, số dư hay trạng thái trận đổi.

### bar-reads-upgrade | AI 7 | `feature-hud-upgrade` đọc danh sách mục, cấp đã mua, giá kế tiếp và mua được hay chưa từ nâng cấp
Lấy theo từng mục để nút hiện đúng cấp đã mua và đúng giá lần mua kế tiếp.

### bar-one-snapshot | AI 6 | Thanh nâng cấp hiện mọi mục từ cùng một bản cập nhật của nâng cấp
Mua xong thì giá mới của mục vừa mua và trạng thái mọi nút đổi theo cùng số dư.

### bar-sends-item | AI 7 | `feature-hud-upgrade` gửi định danh mục được chạm cho nâng cấp để xin mua
Định danh giữ đúng đối tượng người chơi chạm, kể cả khi cách xếp nút đổi.
