---
based-on: 4ab96ef15d1f
refs: feature-run/run-pause@f8e9d49f5079, feature-hud/hud-layout/layout-spec@ebefd7f8b116
---
# Nút tạm dừng trên HUD

HUD có 1 nút tạm dừng với biểu tượng hai vạch đứng. Chạm khi trận đang đánh thì báo `feature-run/run-pause`; lúc khác chạm không có tác dụng.

## Ý cốt lõi

### pause-icon | AI 7 | Có đúng 1 nút, biểu tượng hai vạch đứng, đại diện cho "tạm dừng trận"; không có chữ

### pause-tap-report | AI 7 | Trận đang đánh: chạm 1 lần là báo "đã bấm tạm dừng" cho `feature-run/run-pause` đúng một lần
Nút không tự dừng trận hay mở popup.

### pause-inactive | AI 6 | Trận đang tạm dừng hoặc lượt đã kết thúc: chạm nút không báo gì
Lúc tạm dừng popup che nút; lúc kết thúc nút vẫn hiện nhưng vô tác dụng.

### pause-shown-all-battle | AI 6 | Nút hiện suốt trận, từ lúc trận dựng xong tới lúc trận bị huỷ
