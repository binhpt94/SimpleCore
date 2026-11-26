---
based-on: ca84fa282d14
by: hud-status=gpt@r2-1#hud-castle-damage, hud-clear-view=gpt@r2-2#phan-vung-mep-man, hud-reads-castle-hp=gpt@r2-2#nguon-mau-thanh, hud-reads-money=gpt@r2-2#nguon-so-du, hud-reads-wave=gpt@r2-2#nguon-so-dot, hud-reads-cooldown=gpt@r2-2#nguon-hoi-chieu, castle-checks-skill=gpt@r2-2#kiem-tra-khi-nhan-yeu-cau, castle-runs-skill=gpt@r2-2#thuc-hien-ki-nang, keep-upgrade-bar-apart=gpt@r2-3#giu-rieng-thanh-nang-cap, hud-resizes-bar-area=gpt@r2-3#cap-lai-vung-nang-cap, hud-locks-by-run=gpt@r2-3#khoa-nut-theo-luot, hud-wave-banner-event=gpt@r2-3#bao-dot-tu-su-kien, hud-sends-skill-id=gpt@r2-3#gui-dung-ky-nang
gpt-dropped: r2-3#gui-yeu-cau-tam-dung=hud-pause
---
# HUD trận

Các thông tin và nút luôn hiện trên màn hình trận, trừ thanh nâng cấp (`feature-hud-upgrade`). Người chơi nhìn HUD để biết tình hình và bấm kĩ năng thành.

## Ý cốt lõi

### hud-status | AI 8 | HUD luôn cho thấy mức hư hại hiện tại của thành, số tiền đang có và đợt đang đánh
Thành giữ nguyên hư hại sang đợt mới; nhìn mức hư hại là biết thành còn trụ được tới đâu để canh lúc cứu nguy.

### hud-reads-castle-hp | AI 7 | HUD đọc máu hiện tại và máu tối đa của thành từ `feature-castle`
Một nguồn cho cả thanh máu và số, kể cả khi thành nâng độ bền giữa trận.

### hud-reads-money | AI 7 | HUD đọc số dư hiện tại của lượt từ `feature-wallet`

### hud-reads-wave | AI 7 | HUD đọc số đợt cần hiện từ `feature-wave`

### hud-wave-banner-event | AI 7 | HUD hiện thông báo đầu đợt khi `feature-wave` báo đợt mới đã bắt đầu
Thông báo xuất hiện đúng lúc đợt thật sự bắt đầu, không phải lúc ô đợt đổi số trong lúc thở.

### hud-reads-cooldown | AI 7 | HUD đọc trạng thái sẵn sàng, thời gian hồi còn lại và tổng hồi chiêu từng kĩ năng từ `feature-castle`
Thành giữ bộ đếm; HUD chỉ định dạng để hiện.

### hud-skills | AI 8 | Nút kĩ năng của thành luôn trên màn hình, cho thấy lúc nào dùng được lại

### hud-sends-skill-id | AI 7 | Chạm nút sẵn sàng thì HUD gửi mã của kĩ năng tương ứng do `feature-castle` cấp
Mỗi nút gọi đúng kĩ năng của nó dù thành đổi thứ tự danh sách.

### castle-checks-skill | AI 7 | `feature-castle` kiểm kĩ năng có dùng được ngay lúc nhận từng yêu cầu từ HUD
Nút có thể còn hiện sẵn sàng khi lần bấm trước vừa bắt đầu hồi chiêu; quyết dựa trên trạng thái của thành.

### castle-runs-skill | AI 7 | `feature-castle` thực hiện ngay kĩ năng khi chấp nhận yêu cầu từ HUD
Bên chấp nhận yêu cầu cũng là bên tạo hiệu ứng trong trận.

### hud-locks-by-run | AI 7 | HUD dùng trạng thái từ `feature-run` để khoá nút kĩ năng khi trận dừng hay lượt đã kết thúc
Nút ngừng nhận chạm ngay khi trạng thái lượt đổi, kể cả trước lúc popup hiện.

### hud-clear-view | AI 7 | HUD chia mép màn cho chính nó và `feature-hud-upgrade`, ngoài dải chiến trường
Cách chia mép chốt ở nhánh bố cục HUD; thanh nâng cấp xếp nút trong vùng được cấp. Màn ngang thấp nên chiến trường hai bên thành phải luôn nhìn rõ.

### hud-resizes-bar-area | AI 6 | HUD cập nhật vùng dành cho `feature-hud-upgrade` khi cỡ màn hay vùng an toàn đổi
Thanh nâng cấp xếp nút và giới hạn vùng chạm theo vùng nhận được.

### keep-upgrade-bar-apart | AI 7 | Giữ `feature-hud-upgrade` là feature riêng lo các nút mua nâng cấp trong trận
HUD giữ bố cục chung; hai bên chỉ cần thống nhất vùng đặt thanh.
