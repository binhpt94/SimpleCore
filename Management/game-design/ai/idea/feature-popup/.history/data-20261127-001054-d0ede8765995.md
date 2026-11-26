---
based-on: b2db85f99dba
by: popup-close-owner=gpt@r2-1#popup-close-owner, popup-show=gpt@r2-1#popup-display-owner, lose-opens-after-result=gpt@r2-2#mo-popup-thua-sau-khi-chot, close-before-report=gpt@r2-2#dong-truoc-khi-bao-lua-chon, freeze-after-lose=gpt@r2-3#dung-tran-sau-khi-thua, handoff-after-dispose=gpt@r2-3#chuyen-lua-chon-sau-khi-don, flow-routes-leave=gpt@r2-3#dinh-tuyen-khi-roi-tran, flow-switches-after-popup=gpt@r2-3#doi-man-sau-popup, back-key-lose=gpt@r2-3#quay-lai-khi-thua
gpt-dropped: r2-2#dich-den-sau-bao-thua=run-routes-after-lose, r2-2#mo-popup-khi-tran-da-dung=pause-opens-after-freeze, r2-3#chot-thua-truoc-tam-dung=lose-before-pause, r2-3#lua-chon-khi-dang-dung=choice-only-when-paused, r2-3#quay-lai-khi-tam-dung=back-key-pause
---
# Dịch vụ popup

Dịch vụ hạ tầng hiện và đóng popup cho các feature khác. Nội dung từng popup là feature riêng (ví dụ `feature-popup-lose`).

## Ý cốt lõi

### popup-show | AI 8 | Mọi feature nhờ dịch vụ này hiện popup của mình; feature sở hữu lo nội dung, dịch vụ lo hiển thị

### popup-close-owner | AI 7 | Feature sở hữu popup quyết định khi nào nhờ dịch vụ đóng popup của mình
Feature sở hữu hiểu lựa chọn của người chơi; dịch vụ chỉ thực hiện việc đóng.

### popup-block | AI 7 | Popup đang mở che lên trên và chặn bấm vào phần bên dưới

### lose-opens-after-result | AI 7 | `feature-popup-lose` nhờ dịch vụ mở popup khi `feature-run` báo đã chốt kết quả thua
Tín hiệu chốt bảo đảm điểm và kỉ lục đã sẵn để hiện.

### freeze-after-lose | AI 7 | `feature-run` giữ trận đứng yên từ lúc chốt thua tới khi dọn xong trận
Người chơi đọc kết quả khi quái, đợt và hồi chiêu đều đã dừng; popup thua vẫn nhận bấm.

### flow-routes-leave | AI 6 | `feature-game-flow` quyết màn đích cho mọi lựa chọn rời trận từ popup thua
Chơi lại về màn chọn đội; về màn bắt đầu thì về màn bắt đầu.

### close-before-report | AI 6 | Popup thua báo lựa chọn cho `feature-run` sau khi dịch vụ đã đóng popup
Lượt chuyển màn khi nền che đã gỡ.

### handoff-after-dispose | AI 6 | `feature-run` chuyển lựa chọn rời trận cho `feature-game-flow` sau khi đã dọn xong trận

### flow-switches-after-popup | AI 6 | `feature-game-flow` đổi màn trong scene Game sau lựa chọn rời trận từ popup

### back-key-lose | AI 7 | `feature-popup-lose` bỏ qua nút Quay lại của điện thoại
Người chơi vẫn phải chọn Chơi lại hoặc Về màn bắt đầu.
