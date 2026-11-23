---
based-on: 78ed2dcc0859
refs: feature-scene-flow/scene-request@646c23aa767e, feature-session-scene/session-steps@70f3b17acc2e, feature-session-scene/session-steps/participant-order@aa6627fee3fe
---
# Màn Loading lúc mở game

Màn Loading hiện tên game và thanh tiến độ, tự sang scene Game khi tải xong. Có giới hạn thời gian hiện ít nhất và lâu nhất; tải lỗi thì dừng ở Loading và báo rõ lỗi.

## Ý cốt lõi

### loading-content | AI 8 | Màn Loading chỉ có tên game và một thanh tiến độ 0-100%, không có nút
Tiến độ tính bằng số feature của scene Game đã tải xong chia cho tổng số feature tham gia. Lớp che lúc chuyển sang Game (`feature-scene-flow/scene-request`) có cùng nội dung, nên người chơi thấy một màn Loading liền mạch. Đã loại: thanh tiến độ giả chạy theo thời gian.

### loading-min-time | AI 7 | Màn Loading hiện ít nhất 1 giây, kể cả khi tải xong sớm hơn
Tránh màn nháy lên rồi mất. Không kéo dài hơn mức này khi đã tải xong.

### loading-max-time | AI 7 | Từ lúc mở game tới lúc thấy màn bắt đầu không quá 5 giây trên điện thoại tầm trung (dự kiến)
Quá 5 giây là lỗi hiệu năng cần báo, không chặn người chơi.

### loading-fail | AI 8 | Một feature tải lỗi thì dừng ở Loading, hiện dòng lỗi nêu tên feature đó, không sang scene Game
Không tự thử lại; người chơi tắt mở lại game. Đã loại: bỏ qua feature lỗi rồi vào Game thiếu thứ.

### game-first-screen | AI 8 | Sang scene Game thì màn đầu tiên người chơi thấy là màn bắt đầu
Không vào thẳng trận hay màn chọn đội.
