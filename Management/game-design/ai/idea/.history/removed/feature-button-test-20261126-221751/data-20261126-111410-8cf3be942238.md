---
based-on: 6181a7c21bf0
---
# Nút thử

Một nút để thử trên màn trận: bấm thì thấy phản hồi ngay. Feature này dùng để chạy thử cả quy trình từ thiết kế, bố cục, code, kiểm tới chơi thử. Nó không phải tính năng chơi của game.

## Ý cốt lõi

### test-purpose | AI 8 | Feature này chủ yếu để thử kiến trúc feature độc lập và quy trình làm game, không thêm gì vào lối chơi
Người yêu cầu ngày 2026-11-26. Thử kiến trúc là chính: một feature đứng riêng, có thư mục code riêng, nối dây vào màn trận mà không đụng feature nào. Giữ nhỏ nhất có thể để mỗi bước của quy trình (thiết kế, frame, code, kiểm, chơi thử) đều có việc thật mà không tốn công.

### test-on-battle | AI 7 | Nút thử nằm trên màn trận của scene Game
Màn trận là nơi có nhiều feature cùng hiện nhất, nên thử ở đó kiểm được cả việc xếp chỗ cạnh HUD.

### test-feedback | AI 8 | Bấm nút thử thì người chơi thấy phản hồi ngay trên màn
Phản hồi thấy được là thứ tester chụp lại và kiểm được.

### test-no-effect | AI 8 | Nút thử không đổi gì trong trận và không gửi gì cho feature khác
Không dừng trận, không đổi tiền, máu, đợt hay điểm. Feature khác không phải biết nút này có mặt.
