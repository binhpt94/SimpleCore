also: feature-save, feature-run, feature-team-select, feature-popup-lose

# Cả bộ feature sau khi thêm feature-save

Ngày 2026-11-26 người quyết tách việc đọc và ghi dữ liệu xuống máy ra khỏi `feature-run`, thành feature mới `feature-save` (Lưu dữ liệu). Các lần hỏi chung trước (`r2-all`, `r2-all-2`, `r2-all-3`) có từ trước khi có feature này.

## Câu hỏi
Xét cả bộ feature, nhất là chỗ `feature-save` chạm `feature-run`, `feature-home`, `feature-popup-lose`, `feature-team-select`, `feature-session-scene`:
1. **Trùng việc:** feature nào còn tự đọc hay ghi dữ liệu xuống máy, hay tự giữ một phần việc mà nay `feature-save` giữ? Việc đó nên thuộc bên nào?
2. **Hở chỗ:** dữ liệu nào cần còn sau khi tắt game (kỉ lục, đội đã chọn lần trước, cài đặt...) mà chưa feature nào nhận là chủ? Ai đọc dữ liệu lúc mở game, ai ghi lúc nào (hết lượt, thoát giữa lượt, tắt app)?
3. **Ranh giới mơ hồ:** ai quyết khi nào kỉ lục đổi, ai hiển thị kỉ lục ở Home và ở PopupLose, ai nhớ đội đã chọn? `feature-session-scene` dọn dữ liệu của lượt có đụng dữ liệu đã lưu không?
4. **Tách, gộp, thêm:** có nên tách, gộp hay thêm feature nào quanh việc lưu không?

Mỗi góp ý ghi rõ feature đích (hoặc tên key đề xuất cho feature mới). Chỗ đã ổn thì không viết gì.

## Yêu cầu riêng của người
- Không ghi dữ liệu theo giai đoạn (bản đầu, MVP, làm trước / để sau).

## Bên hỏi muốn
@GameDesigner cần các góp ý cụ thể về ranh giới để đưa vào node, trước khi hỏi riêng `feature-save`.
