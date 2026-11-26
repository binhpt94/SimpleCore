also: feature-battlefield, feature-hero, feature-hud, feature-popup

# Feature thử Spine: đủ ý cốt lõi chưa, thiếu kiểm tra nào, nên tách gộp node con nào, ranh giới với chiến trường, hero, HUD, popup

`feature-spine-test` là feature thử nhân vật Spine (animation xương 2D) trên màn trận của game 2D nhìn ngang. Nó không phải tính năng chơi của game.

## Ràng buộc của người (người yêu cầu ngày 2026-11-26, phải giữ)
- Là một feature riêng.
- Chỉ để thử, không thêm gì vào lối chơi.

## Câu hỏi
1. **Ý cốt lõi đã đủ chưa?** So với mục đích thử Spine trước khi quyết có dùng Spine cho hero, quái hay không: node còn thiếu ý cốt lõi nào (ví dụ: tiêu chí đạt hay không đạt của cả lần thử, kết quả thử được ghi lại thế nào, ai xem và quyết)? Ý nào còn mơ hồ?
2. **Còn thiếu điều cần kiểm nào** về Spine trong game 2D nhìn ngang? Xét các node con hiện có (danh sách điều cần kiểm, cheat) và các rủi ro thường gặp khi trộn Spine với sprite: thứ tự vẽ, material và shader, viền và bảng màu so với style 2D, cỡ ở các tỉ lệ màn, quay mặt, chuyển và trộn animation, hiệu năng, nhân vật bị che hay bị cắt bởi camera. Điều nào thiếu, điều nào thừa so với mức cần để thử?
3. **Node con:** các node con hiện có đã chia đúng chưa? Nên tách, gộp, hay thêm node con nào?
4. **Quan hệ với `feature-battlefield`, `feature-hero`, `feature-hud`, `feature-popup`:** cần làm rõ gì để ranh giới không mơ hồ? Ví dụ: nhân vật thử theo lớp vẽ và camera của chiến trường thế nào, đứng ở đâu để không chồng hero, quái và hai feature thử khác; so cỡ với hero theo tiêu chí gì; HUD và popup che nhân vật thế nào khi trận dừng; nút đổi animation nằm ở đâu so với HUD.

Bên hỏi (@GameDesigner) cần các góp ý cụ thể để đưa vào node trước khi đi tiếp vòng 3.
