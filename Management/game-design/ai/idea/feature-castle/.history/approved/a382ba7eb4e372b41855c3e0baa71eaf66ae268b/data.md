---
based-on: b2db85f99dba
by: castle-damage-between-waves=gpt@r2-1#castle-damage-between-waves, castle-auto-attack=gpt@r2-1#castle-defends-nearby
---
# Thành

Tòa thành đứng giữa màn hình: thứ người chơi phải giữ, đồng thời là một chiến binh tự đánh quái và có kĩ năng người chơi bấm để cứu nguy. Thành bị phá là hết lượt.

## Ý cốt lõi

### castle-health | AI 8 | Thành là thứ phải giữ: quái tới được thì đánh thành, thành bị phá là thua
Báo cho `feature-run` khi thành bị phá.

### castle-damage-between-waves | AI 7 | Thành giữ nguyên mức hư hại khi sang đợt quái kế tiếp
Giữ thành ít hư hại trong đợt này thì đợt sau chống đỡ tốt hơn.

### castle-auto-attack | AI 8 | Thành tự đánh quái áp sát từ cả hai phía, không cần người chơi bấm
Vai trò của đòn tự đánh là phòng thủ quanh thành; quái không cần vượt qua hero thì thành mới đánh. Đã loại: người chơi chạm vào quái để thành bắn.

### castle-skills | AI 7 | Thành có kĩ năng người chơi bấm để dùng, dùng xong phải chờ hồi mới dùng lại
Đây là thao tác chủ động chính trong trận. Bấm là dùng ngay, không cần chọn vị trí. Đã loại (người loại ở vòng 1): thành không đánh, không kĩ năng.

### castle-hero-base | AI 7 | Thành là nơi đội hero xuất phát khi vào trận và hồi sinh khi gục

### castle-upgrades | AI 7 | Thành mạnh lên qua nâng cấp trong trận
Nâng được độ bền, sức đánh và kĩ năng của thành; mua qua `feature-upgrade`.
