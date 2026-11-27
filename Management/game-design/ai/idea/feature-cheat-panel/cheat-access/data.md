---
based-on: 75d09f183702
refs: feature-cheat-panel/cheat-states@1a6897ef3a27
---
# Nút, panel và công tắc cheat

Quan hệ giữa config bật tắt, nút cheat và panel cheat: config quyết có nút hay không, nút mở và đóng panel, panel nằm trên mọi thứ khác.

## Ý cốt lõi

### access-one-switch | AI 8 | Một config duy nhất quyết cả nút lẫn panel: bật thì có nút, tắt thì không có cả hai
Không có cách nào khác để mở panel khi config tắt.

### access-switch-at-start | AI 7 | Config được đọc một lần lúc game khởi động
Đổi config thì có hiệu lực ở lần mở game sau. Đã loại: đổi trong lúc chơi (cần thêm một chỗ bật tắt nằm ngoài panel).

### access-button-every-scene | AI 8 | Khi config bật, nút cheat có mặt ở mọi scene và mọi màn của game
Kể cả màn Loading, màn bắt đầu, trận và lúc popup đang mở.

### access-button-on-top | AI 8 | Nút cheat nằm trên mọi phần khác của game, kể cả popup
Popup đang mở vẫn chạm được nút cheat.

### access-button-opens-panel | AI 8 | Chạm nút cheat thì mở panel cheat; chạm lần nữa thì đóng
Một nút làm cả hai việc.

### access-panel-blocks-below | AI 8 | Panel đang mở thì phần game bên dưới không nhận chạm
Tránh chạm nhầm vào trận hay popup khi đang chọn state.

### access-game-keeps-running | AI 6 | Mở panel không dừng game; trận vẫn chạy phía sau
Đã loại: dừng trận khi mở panel (phải nối thêm với `feature-sim-clock`, việc test không cần).
