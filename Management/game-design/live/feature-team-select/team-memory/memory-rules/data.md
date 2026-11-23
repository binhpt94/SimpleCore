---
based-on: 4a24db16dcc6
refs: feature-run/run-start/start-values@ecc0af822edd
---
# Đội nhớ và đội mặc định

Lần mở game đầu, 3 chỗ đã có sẵn đội mặc định; từ lượt thứ hai, 3 chỗ lấy đúng đội vừa vào trận gần nhất. Đội nhớ mất khi tắt game.

## Ý cốt lõi

### memory-default-team | AI 7 | Chưa có đội nhớ thì 3 chỗ xếp sẵn đội mặc định: Hiệp sĩ, Đấu sĩ rìu, Cung thủ
Người chơi mới bấm "Vào trận" ngay được, không phải hiểu luật chọn trước. Đã loại: để trống 3 chỗ (người mới không biết bắt đầu từ đâu).

### memory-save-on-start | AI 7 | Bấm "Vào trận" thì 3 hero đang ở 3 chỗ thành đội nhớ, giữ đúng thứ tự chỗ

### memory-prefill-order | AI 7 | Mở màn chọn đội thì chỗ 1, 2, 3 lấy đúng hero ở chỗ 1, 2, 3 của đội nhớ
Người chơi đổi được như bình thường; đổi xong không bấm "Vào trận" thì đội nhớ không đổi.

### memory-forget-on-quit | AI 6 | Tắt game thì đội nhớ mất; mở lại thì dùng đội mặc định
Không ghi đội xuống máy.
