---
based-on: 1a6897ef3a27
frame: none
---
# Dòng state và cách áp state

Thứ tự và chữ của các dòng trong panel cheat, và game làm gì khi chạm một dòng: tải lại scene chứa state và để feature sở hữu dựng state đó.

## Ý cốt lõi

### rows-order | AI 7 | Dòng "Chơi bình thường" đứng đầu, sau đó là các dòng state xếp theo tên feature A tới Z
Trong một feature, các state theo đúng thứ tự ý ở node cheat của feature đó.

### rows-label | AI 7 | Mỗi dòng state ghi "<tên feature>: <tiêu đề state>"
Lấy tên feature và tiêu đề ý của node cheat, ví dụ "Popup thua: Popup thua đang mở, lượt vừa phá kỉ lục".

### rows-mark-active | AI 6 | Dòng của state đang chạy được đánh dấu đang chọn; không có state thì dòng "Chơi bình thường" được đánh dấu

### apply-reload-scene | AI 8 | Chạm một dòng state thì game tải lại scene chứa state đó, và feature sở hữu dựng state ngay khi scene chạy
Đang ở scene khác thì chuyển sang qua `feature-scene-flow`. Đang ở đúng scene thì vẫn tải lại, để state luôn dựng từ đầu như khi chọn trên asset rồi bấm Play.

### apply-same-again | AI 6 | Chạm lại dòng của state đang chạy thì dựng lại state đó từ đầu

### apply-normal-restart | AI 7 | Chạm "Chơi bình thường" thì bỏ state cheat và game chạy lại từ scene đầu tiên như lúc mới mở
Không giữ gì của state cheat vừa chạy.

### apply-no-save | AI 8 | State cheat không ghi gì xuống máy
Kỉ lục ép trong một state không đè kỉ lục thật đã lưu.

### apply-missing-state | AI 7 | State có ở node cheat nhưng bản build không dựng được thì không có dòng
Ví dụ bản build không mang code cheat của feature đó.
