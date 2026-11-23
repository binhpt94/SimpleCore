---
based-on: 469d9009792c
---
# Thành

Tòa thành đứng giữa màn hình: thứ người chơi phải giữ, đồng thời là một chiến binh tự đánh quái và có kĩ năng người chơi bấm để cứu nguy. Thành bị phá là hết lượt.

## Ý cốt lõi

### castle-health | AI 8 | Thành là thứ phải giữ: quái tới được thì đánh thành, thành bị phá là thua
Báo cho `feature-run` khi thành bị phá.

### castle-auto-attack | AI 7 | Thành tự đánh quái ở cả hai bên, không cần người chơi bấm
Đánh quái lọt qua hàng hero. Đã loại: người chơi chạm vào quái để thành bắn.

### castle-skills | AI 7 | Thành có kĩ năng người chơi bấm để dùng, dùng xong phải chờ hồi mới dùng lại
Đây là thao tác chủ động chính trong trận. Bấm là dùng ngay, không cần chọn vị trí. Đã loại (người loại ở vòng 1): thành không đánh, không kĩ năng.

### castle-hero-base | AI 7 | Thành là nơi đội hero xuất phát khi vào trận và hồi sinh khi gục

### castle-upgrades | AI 7 | Thành mạnh lên qua nâng cấp trong trận
Nâng được độ bền, sức đánh và kĩ năng của thành; mua qua `feature-upgrade`.
