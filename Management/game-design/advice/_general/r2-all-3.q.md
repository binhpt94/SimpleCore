also: feature-button-test, feature-cube-test, feature-spine-test

# Cả bộ feature sau khi thêm ba feature thử

Ngày 2026-11-26 người thêm ba feature thử: `feature-button-test`, `feature-cube-test`, `feature-spine-test`. Chúng để thử kiến trúc, quy trình làm game và cách hiển thị, không phải tính năng chơi của game. Các lần hỏi chung trước (`r2-all`, `r2-all-2`) có từ trước khi có ba feature này.

## Câu hỏi
Xét cả bộ feature hiện có, gồm feature thật và ba feature thử:
1. **Trùng việc:** feature nào làm cùng một việc với feature khác? Nhất là giữa ba feature thử với nhau, và giữa feature thử với feature thật đang giữ màn trận, chiến trường, HUD, popup.
2. **Thiếu feature:** còn thiếu feature nào, thật hay thử? Ví dụ một việc mà các feature thử cần nhưng chưa ai giữ.
3. **Ranh giới giữa feature thử và feature thật:** chỗ nào còn mơ hồ? Ai giữ chỗ trên màn, thứ tự vẽ, việc chặn bấm khi popup mở? Feature thử có phải biết feature thật có mặt không, và ngược lại? Khi bỏ feature thử đi thì feature thật có bị ảnh hưởng không?

Bên hỏi (@GameDesigner) cần các góp ý cụ thể để đưa vào node trước khi hỏi riêng từng feature thử.
