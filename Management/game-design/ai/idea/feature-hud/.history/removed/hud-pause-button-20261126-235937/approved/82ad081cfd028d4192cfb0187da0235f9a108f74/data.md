---
based-on: 628a4bda5a29
refs: feature-run/run-pause@331cbd30e30e, feature-hud/hud-layout@e541be0cca19, feature-popup/popup-layer@91d4f017d2fb
---
# Nút tạm dừng

Một nút trên HUD trận để tạm dừng. Bấm là trận dừng và popup tạm dừng hiện.

## Ý cốt lõi

### pause-button-one | AI 7 | HUD có một nút tạm dừng, chạm một lần là tạm dừng
Nút chỉ báo "đã bấm" cho `feature-run/run-pause`; trận dừng và popup hiện là việc của `feature-run/run-pause` và `feature-popup-pause`.

### pause-button-apart | AI 6 | Nút tạm dừng nằm xa nút kĩ năng và thanh nâng cấp
Tránh chạm nhầm giữa lúc đang vội bấm kĩ năng hay mua nâng cấp. Đã loại: đặt cạnh nút kĩ năng.

### pause-button-active | AI 6 | Nút tạm dừng chỉ bấm được khi trận đang đánh
Đang tạm dừng thì popup che nút (`feature-popup/popup-layer`); lượt đã kết thúc thì nút không còn tác dụng.
