also: feature-hud, feature-popup

# Feature nút thử: ranh giới, ý thiếu hoặc thừa, tách hay gộp

`feature-button-test` là feature mới, người yêu cầu ngày 2026-11-26. Mục đích: chạy thử trọn quy trình làm game, từ thiết kế, bố cục, code, kiểm tới chơi thử. Nó không phải tính năng chơi của game.

## Ràng buộc của người (người yêu cầu, phải giữ)
- Giữ feature nhỏ nhất có thể.
- Có một nút bấm, bấm thì có phản hồi thấy được.
- Đặt ở game scene.

## Câu hỏi
1. **Ranh giới với `feature-hud` và `feature-popup`:** nút thử nằm cùng màn với HUD, và popup có thể mở đè lên nó. Chỗ giao nhau có điểm nào còn mơ hồ, ví dụ ai giữ chỗ trên màn, ai quyết việc chặn bấm khi popup mở, hay nút có phải biết HUD hoặc popup có mặt không?
2. **Ý thiếu hoặc thừa:** với mục đích chạy thử quy trình, node còn thiếu quyết định nào mà vòng 3 và 4 sẽ cần? Có ý nào thừa, vượt quá mức "nhỏ nhất có thể"?
3. **Tách hay gộp:** nên giữ thành một feature riêng, hay gộp vào feature có sẵn? Hoặc có nên tách thêm phần nào không?

Bên hỏi (@GameDesigner) cần các góp ý cụ thể để đưa vào node trước khi đi tiếp vòng 3.
