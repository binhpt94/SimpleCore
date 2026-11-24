---
based-on: aebbf6890808
refs: feature-popup-pause/pause-popup/pause-popup-spec@4db8a9f181fd, feature-popup/popup-layer/stack-rules@fa85847e35ee, feature-hud/hud-layout/layout-spec@ebefd7f8b116, feature-run/run-pause/pause-rules@2f184465270c
---
# Cheat: popup tạm dừng

State dựng nhanh popup tạm dừng ở bản dev để test, Play và gen ảnh concept, không phải vào trận rồi bấm tạm dừng.

## Ý cốt lõi

### paused-main | AI 7 | Trận đang tạm dừng, popup tạm dừng mở ở bước chính
Ép: trận đứng yên ở đợt 5, popup tạm dừng mở ở bước chính. Có tiêu đề "Tạm dừng", nút "Tiếp tục", nút "Bỏ lượt".

### paused-confirm | AI 7 | Trận đang tạm dừng, popup tạm dừng ở bước hỏi xác nhận bỏ lượt
Ép: trận đứng yên ở đợt 5, popup tạm dừng đã chuyển sang bước hỏi. Có câu hỏi "Bỏ lượt này? Điểm tính theo số đợt đã dọn.", nút "Bỏ lượt", nút "Không".
