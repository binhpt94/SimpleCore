---
based-on: 70f3b17acc2e
refs: feature-scene-flow/scene-list/boot-loading@77148b90014b, feature-run/run-start@31cb7598b529, feature-balance/balance-config@6934878b1b70
---
# Danh sách tham gia và xử lí lỗi từng bước

Chốt danh sách và thứ tự feature tham gia của scene Loading và scene Game, giới hạn thời gian từng bước, và việc phải làm khi một feature lỗi ở bước tải, chạy hoặc huỷ.

## Ý cốt lõi

### loading-list | AI 8 | Scene Loading chỉ có một feature tham gia: `feature-scene-flow` (màn Loading)

### game-list | AI 8 | Scene Game có 14 feature tham gia theo thứ tự: popup, run, wallet, castle, hero, enemy, wave, upgrade, home, team-select, hud, hud-upgrade, popup-lose, popup-pause
Dịch vụ và luật trận đứng trước, màn hình và popup đứng sau, để feature đứng sau dùng được thứ feature đứng trước đã chuẩn bị. `feature-balance` không tham gia: nó chỉ là số liệu nằm trong cấu hình của từng feature.

### load-timeout | AI 7 | Mỗi feature tải tối đa 10 giây (dự kiến); quá thì coi là tải lỗi
Các feature tải lần lượt theo thứ tự danh sách.

### run-no-wait | AI 7 | Bước chạy không tải thêm gì và xong trong cùng khung hình
Mọi thứ cần cho view đã có từ bước tải, nên view hiện ra cùng lúc, không thiếu hình.

### load-or-run-fail | AI 8 | Một feature lỗi ở bước tải hoặc chạy thì scene dừng, các feature đã qua bước đó được huỷ theo thứ tự ngược, lỗi nêu tên feature lỗi
Không feature nào sau nó được tải hay chạy. Đã loại: bỏ qua feature lỗi rồi chạy tiếp (scene thiếu thứ, lỗi khó lần).

### dispose-never-stops | AI 7 | Một feature lỗi ở bước huỷ thì ghi lỗi nêu tên nó, vẫn huỷ tiếp các feature còn lại
Bước huỷ phải đi hết danh sách để không còn gì sống sót.
