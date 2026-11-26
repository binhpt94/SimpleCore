also: feature-hud, feature-popup

# Feature nút thử: thiếu, trùng hay lệch gì so với mục đích người đặt

`feature-button-test` là feature mới. Người yêu cầu nó ngày 2026-11-26, với mục đích: **thử kiến trúc feature độc lập và quy trình làm game** (thiết kế, bố cục, code, kiểm, chơi thử). Nó không phải tính năng chơi của game.

## Ràng buộc của người (người yêu cầu, phải giữ)
- Giữ feature nhỏ nhất có thể.
- Có một nút bấm, bấm thì có phản hồi thấy được.
- Đặt ở game scene.

## Câu hỏi
So với mục đích trên, node feature này:
1. **Còn thiếu** quyết định nào mà vòng 3 và 4 sẽ cần? Hay thiếu quyết định nào để lần thử này thật sự kiểm được kiến trúc feature độc lập (thư mục code riêng, nối dây vào màn, không phụ thuộc feature khác) và đủ các bước của quy trình?
2. **Trùng** việc với feature nào có sẵn, nhất là `feature-hud` (cùng màn trận) và `feature-popup` (popup mở đè lên)? Ở chỗ giao nhau, ai giữ chỗ trên màn, ai quyết việc chặn bấm khi popup mở?
3. **Lệch** khỏi mục đích ở đâu: ý nào thừa, vượt quá mức "nhỏ nhất có thể", hoặc kéo feature thành tính năng chơi?
4. Nên giữ thành một feature riêng, gộp vào feature có sẵn, hay tách thêm?

Bên hỏi (@GameDesigner) cần các góp ý cụ thể để đưa vào node trước khi đi tiếp vòng 3.
