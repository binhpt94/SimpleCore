---
based-on: 2830f8d52b13
refs: feature-home/cheat@1881bd47e941, feature-popup-lose/cheat@683571cd6600, feature-run/run-score/score-rules@1090f4b26dd9
frame: none
---
# Luật đọc ghi bản ghi lưu

Bản ghi lưu mang phiên bản dữ liệu 1 và một số nguyên kỉ lục. Đọc, ghi xong ngay trong lần gọi; ghi không bao giờ để lại bản ghi dở; bản ghi lạ hay hỏng thì đọc báo chưa có và không bị xoá.

## Ý cốt lõi

### rules-version-one | AI 8 | Phiên bản dữ liệu hiện tại là 1; mỗi lần ghi đều ghi kèm số 1
Chưa có phiên bản nào cũ hơn, nên chưa có dạng cũ nào phải chuyển.

### rules-record-int | AI 8 | Kỉ lục được cất là số nguyên 32 bit, trả lại đúng số đã cất, kể cả số âm
Lưu dữ liệu không xét số âm; `feature-run` coi kỉ lục âm là 0.

### rules-read-cases | AI 8 | Đọc: phiên bản 1 và kỉ lục là số nguyên thì trả kỉ lục; mọi trường hợp khác trả chưa có
Khác gồm: không có bản ghi, thiếu phiên bản hay kỉ lục, phiên bản không phải số nguyên dương, kỉ lục không phải số nguyên, phiên bản lớn hơn 1.

### rules-instant | AI 7 | Đọc và ghi trả kết quả ngay trong lần gọi, bên gọi không phải chờ khung hình sau
Chỉ có một số nguyên nên ghi rất nhẹ (dự kiến dưới 10 ms). Nhờ vậy kỉ lục đã ghi xong trước khi popup thua hiện.

### rules-write-whole | AI 7 | Tắt game ngay giữa lúc ghi thì lần mở sau đọc được bản cũ hoặc bản mới, không bao giờ ra chưa có vì ghi dở

### rules-newer-reject | AI 6 | Trên máy là bản ghi có phiên bản lớn hơn 1 thì mọi lần ghi báo thất bại và bản ghi giữ nguyên

### rules-broken-kept | AI 6 | Bản ghi hỏng không bị xoá khi đọc; chỉ bị thay khi có lần ghi thành công

### rules-fail-silent | AI 7 | Ghi thất bại không hiện gì cho người chơi; bản dev ghi một dòng cảnh báo vào log kèm lí do
