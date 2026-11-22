---
based-on: d8a90fd4adc0
---
# Tạm dừng trận

Trạng thái tạm dừng của lượt: trận đứng yên hoàn toàn và người chơi không tác động được vào trận cho tới khi tiếp tục. Nội dung popup nằm ở `feature-popup-pause`.

## Ý cốt lõi

### pause-states | AI 7 | Trận có hai trạng thái khi chưa kết thúc: đang đánh và tạm dừng
Bấm nút tạm dừng (`feature-hud`) thì sang tạm dừng; bấm tiếp tục thì về đang đánh.

### pause-what-stops | AI 7 | Lúc tạm dừng mọi thứ đứng yên: quái, hero, thành, đợt, hồi sinh và hồi chiêu
Không thứ gì trong trận trôi theo thời gian khi đang tạm dừng. Đã loại: hồi chiêu vẫn chạy (người chơi lợi dụng dừng để chờ kĩ năng).

### pause-no-actions | AI 7 | Lúc tạm dừng không mua nâng cấp và không bấm kĩ năng được
Giữ áp lực quyết định ngay giữa trận. Đã loại: dừng để mua thoải mái (gần với "chỉ nâng cấp giữa hai đợt" người đã loại ở vòng 1).

### pause-on-leave | AI 6 | Rời khỏi game giữa trận thì trận tự tạm dừng
Người chơi điện thoại bị ngắt (cuộc gọi, chuyển ứng dụng); quay lại thấy popup tạm dừng chứ không thấy thành đã bị phá.

### pause-quit | AI 7 | Bỏ lượt khi đang tạm dừng là kết thúc lượt, tính điểm như thua
Đợt đang dở không tính (`feature-run/run-score`); sau đó về màn bắt đầu (`feature-run/run-flow`).
