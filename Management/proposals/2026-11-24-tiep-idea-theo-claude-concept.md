# Làm tiếp mức IDEA theo claude-concept

Người giao (2026-11-24): claude-concept đã đủ cho các node vòng 4, các phiên tầng dưới làm tiếp.

## Căn cứ
- Claude-concept đã giao ở `Resources/_design/<node>/claudeimage/`: Castle, Enemy, Hero, Wave, Hud, HudUpgrade, Popup, PopupLose, PopupPause, SceneFlow, TeamSelect, Home (cả node cheat của Hud, HudUpgrade, Popup, PopupLose, PopupPause, SceneFlow, TeamSelect, Home).
- `gd_pack.py <Tên>` in mục Claude concept và bảng phần: đó là danh sách sprite feature cần.
- Mức IDEA (`.claude/rules/codelead.md`, mục Tiến độ): hình có nghĩa, ảnh @ClaudeImage là đủ, anim bằng scale, move, rotate, đổi màu. Không chờ ảnh @Codex.

## Việc mỗi phiên
Chung cho mọi phiên có view:
1. Chạy lại `gd_pack.py <Tên>` (gói cũ trong `.pack` có thể đã lỗi thời), đọc mục Claude concept và bảng phần.
2. Còn ô màu tạm mà chưa có art nào: đặt order `tool: claudeimage`, `kind: image`, mỗi phần trong bảng một dòng, kích thước theo `w`x`h` của phần. `studio.py check`, rồi nhắn `ClaudeImage`.
3. Có hàng rồi thì chấm (`studio.py review`), thay ô màu tạm bằng sprite, biên dịch, `validate.py --feature <Tên>`, báo cáo.

| Phiên | Đang ở đâu | Việc tiếp |
|---|---|---|
| PopupPause | chưa có code | dựng popup tạm dừng (node `pause-popup-spec`) và cheat của nó; nhờ @CodeLead dựng khung nếu chưa có; sprite theo claude-concept |
| TeamSelect | chưa có code | dựng thẻ hero, ba chỗ trong đội, nhớ đội, cheat; sprite theo claude-concept |
| Home | chưa có code | dựng màn bắt đầu và cheat; sprite theo claude-concept |
| Hud | code xong, IDEA layout và nút tạm dừng còn `todo` | làm nút tạm dừng (gọi Run qua Contracts), cheat của Hud, thay ô màu bằng sprite theo claude-concept |
| HudUpgrade | code xong | cheat của HudUpgrade; dùng art Codex `bar-icons` đã giao (đề xuất `thay-art-codex`) |
| PopupLose | code xong | sprite khung và nút theo claude-concept; font chờ người chọn |
| SceneFlow | code xong | cheat màn Loading; sprite theo claude-concept |
| _Services/Popup | chờ node cheat | node `feature-popup/cheat` đã có bản mới: người Sync sang `live`, rồi Popup làm `Popup/DevSample` |
| Run | luồng và điểm `doing` | luật tạm dừng (`pause-rules`) cùng lúc PopupPause và Hud làm nút |
| Castle, Enemy, Hero | code xong | làm theo đề xuất `thay-art-codex` (art Codex đã giao) |
| Wave | `gap-timer` còn `todo` | báo @CodeLead đã làm lúc thở 3 giây để kiểm và đánh tiến độ |
| Balance | chưa có code | số liệu đã nằm trong config của từng feature; tạm không làm ở IDEA, người quyết |
| @CodeLead | đang Play toàn trận | dựng khung cho PopupPause, TeamSelect, Home nếu thiếu; nối installer khi các phiên báo xong; kiểm cheat Hud, HudUpgrade, PopupLose |

## Điểm cần người quyết
- Phiên đang mở lúc này chỉ có Wave, PopupPause, Core. Các phiên còn lại cần người mở để nhận việc.
- Font TMP có dấu tiếng Việt dùng chung (PopupLose, @CodeLead đã hỏi).
- Sync node `feature-popup/cheat` sang `live`.
- Balance có cần code riêng ở IDEA không.
