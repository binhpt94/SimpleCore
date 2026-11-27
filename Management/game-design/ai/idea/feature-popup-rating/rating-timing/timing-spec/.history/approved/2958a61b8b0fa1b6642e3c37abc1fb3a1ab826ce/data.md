---
based-on: 60bcc7e2fce2
refs: feature-run/run-score/score-rules@2ff455851b82, feature-run/run-flow/flow-rules@593e3d21d6c1, feature-popup/popup-layer/stack-rules@5f909869fc3d
frame: none
---
# Lúc mời: thời điểm, cờ đã mời và trường hợp biên

Xin mở lời mời 0.5 giây sau khi popup thua hiện, nếu lượt vừa phá kỉ lục và lần mở game này chưa mời. Cờ "đã mời" chỉ nằm trong bộ nhớ, bật khi popup mời đã hiện.

## Ý cốt lõi

### start-signal | AI 8 | Bắt đầu đếm khi `feature-run` báo đã chốt kết quả thua, cùng tín hiệu popup thua dùng để mở
Lúc đó điểm, kỉ lục và cờ "kỉ lục mới" đã chốt (`feature-run/run-score/score-rules`).

### invite-delay | AI 7 | Xin mở lời mời 1.5 giây sau lúc trận dừng, tức 0.5 giây sau khi popup thua hiện
Popup thua hiện sau 1 giây (`feature-run/run-flow/flow-rules#lose-popup-delay`); thêm 0.5 giây (dự kiến) để người chơi kịp thấy dòng "Kỉ lục mới!" rồi mới bị che.

### check-flag | AI 8 | Đến lúc xin mở, cờ "kỉ lục mới" tắt hoặc cờ "đã mời" bật thì không mời

### invited-flag | AI 8 | Cờ "đã mời" bật khi popup mời đã hiện, và giữ tới khi tắt game
Chỉ nằm trong bộ nhớ, không lưu xuống máy. Mở game thì cờ tắt.

### cancel-on-leave | AI 7 | Người chơi đã chạm nút trên popup thua trước lúc xin mở thì huỷ lần mời này, cờ "đã mời" giữ nguyên tắt
Huỷ khi `feature-run` bắt đầu huỷ trận theo lựa chọn rời trận; lời mời không hiện đè lên trận mới.

### refused-no-count | AI 7 | Dịch vụ từ chối mở (đã đủ popup chồng, id đang mở) thì không mời, cờ "đã mời" giữ nguyên tắt
Theo `feature-popup/popup-layer/stack-rules`; ghi cảnh báo trong log.

### background-pause | AI 6 | Thời gian ở ngoài game không tính vào 1.5 giây chờ
Quay lại game thì đếm tiếp phần còn lại.

### lose-under-input | AI 8 | Trong lúc lời mời mở, popup thua không nhận chạm; nút quay lại của máy tới lời mời
Theo luật chỉ popup trên cùng nhận bấm của dịch vụ popup.
