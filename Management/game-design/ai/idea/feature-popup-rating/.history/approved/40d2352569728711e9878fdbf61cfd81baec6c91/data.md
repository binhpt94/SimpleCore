---
based-on: ca84fa282d14
by: rating-neutral-text=gpt@feature-popup-rating/r2-1#rating-neutral-text
---
# Popup mời đánh giá

Popup mời người chơi đánh giá game trên Google Play. Bấm đánh giá thì mở trang của game trên Google Play; không muốn thì đóng và chơi tiếp.

## Ý cốt lõi

### rating-role | AI 8 | Popup mời đánh giá là một feature riêng, nhờ `feature-popup` hiện và đóng
Feature này lo nội dung và lúc mời; dịch vụ popup lo hiển thị.

### rating-good-moment | AI 7 | Chỉ mời sau một lượt người chơi vừa phá kỉ lục
Lúc người chơi đang vui thì lời mời ít phiền nhất. `feature-run` là bên biết lượt nào phá kỉ lục. Đã loại: mời ngay lúc mở game, mời sau mỗi lượt thua.

### rating-after-lose-popup | AI 7 | Lời mời hiện sau khi `feature-popup-lose` đã đóng, không chen vào popup thua
Người chơi xem kết quả và chọn Chơi lại hay Về màn bắt đầu trước, rồi mới được mời.

### rating-two-choices | AI 8 | Popup có hai lựa chọn: đánh giá, hoặc để sau
Đánh giá thì mở trang của game trên Google Play. Để sau thì đóng popup, người chơi đi tiếp màn đã chọn.

### rating-no-reward | AI 8 | Không tặng thưởng và không ép số sao khi mời đánh giá
Google Play cấm đổi quà lấy đánh giá; người chơi tự chọn số sao trên Google Play. Đã loại: cho chọn sao trong game rồi chỉ đưa người chọn 5 sao sang Google Play.

### rating-not-nagging | AI 7 | Không mời lại liên tục: mỗi lần mở game mời nhiều nhất một lần
Mời nhiều làm người chơi khó chịu và bấm đóng theo thói quen.

### rating-neutral-text | AI 8 | Lời mời dùng chữ trung tính, không hỏi mức hài lòng trước khi mở Google Play
Chữ kiểu "Đánh giá game trên Google Play". Dẫn tới 5 sao hay hỏi hài lòng để lọc người được đưa sang cửa hàng có rủi ro bị coi là thao túng đánh giá.
