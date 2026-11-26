also: feature-cube-test

# Feature thử Spine: cần kiểm gì, cách đổi animation, rủi ro khi trộn với sprite

`feature-spine-test` là feature mới, người yêu cầu ngày 2026-11-26. Mục đích: thử nhân vật Spine (animation xương 2D) trong game 2D nhìn ngang. Nó không phải tính năng chơi của game.

## Ràng buộc của người (người yêu cầu, phải giữ)
- Là một feature riêng.
- Thuộc giai đoạn IDEA.
- Chỉ để thử, không thêm gì vào lối chơi.

## Câu hỏi
So với mục đích trên, node feature này:
1. **Cần kiểm những gì** về nhân vật Spine trong game 2D nhìn ngang? Ví dụ thứ tự vẽ so với sprite, cỡ ở các tỉ lệ màn, quay mặt trái phải, chuyển giữa các animation, đổi skin, hiệu năng. Điều nào còn thiếu, điều nào thừa so với mức cần để thử?
2. **Người thử đổi animation thế nào:** cách nào đơn giản mà đủ để xem hết các animation? Kết quả lần thử là gì, xem thế nào thì coi là đạt hay không đạt?
3. **Rủi ro khi trộn Spine với sprite:** thứ tự vẽ, material và shader, viền và bảng màu so với style 2D, nằm dưới popup, hiệu ứng vẽ đè. Rủi ro nào nên thành một điều cần kiểm?
4. **So với `feature-cube-test`** (thử model 3D giữa sprite 2D): có trùng việc không? Hai feature thử có nên dùng chung một cách đặt vật thử và đánh giá không, hay giữ hoàn toàn tách? Còn thiếu feature hay node con nào?

Bên hỏi (@GameDesigner) cần các góp ý cụ thể để đưa vào node trước khi đi tiếp vòng 3.
