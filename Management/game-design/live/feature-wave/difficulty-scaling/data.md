---
based-on: 1a74f8c24f86
refs: feature-wave/spawning@8c9e1c247bf1, feature-balance/balance-difficulty-curve@f1f04ceb7020, feature-wave/wave-clear@b7789c127bb4, feature-enemy/enemy-list@10b291cde6dc
---
# Độ khó theo đợt

Độ khó của một đợt chỉ tính từ số đợt, không viết tay từng đợt, nên chơi được vô tận. Đợt sau đông hơn, quái lì và đau hơn, và có thêm loại quái mới.

## Ý cốt lõi

### scale-by-number | AI 8 | Độ khó của đợt chỉ phụ thuộc số đợt, tính bằng quy tắc chứ không viết tay
Cùng số đợt thì luôn khó như nhau, ở mọi lượt. Đã loại: bảng đợt viết tay (không vô tận được); độ khó theo sức đội người chơi (người chơi giỏi bị phạt).

### scale-count | AI 8 | Đợt sau có nhiều quái hơn đợt trước và quái ra dày hơn
Áp lực thấy bằng mắt: dải đất đông dần.

### scale-stats | AI 7 | Quái đợt sau có máu và sức đánh cao hơn; tốc độ đánh, tầm đánh, tốc độ di chuyển giữ theo loại quái
Giữ "quái nào trông ra quái nấy": quái nhanh vẫn nhanh, quái trâu vẫn chậm. Đã loại: tăng mọi chỉ số (quái đợt muộn chạy quá nhanh, khó nhìn).

### scale-types | AI 7 | Loại quái mở dần theo đợt; đợt đầu chỉ có loại đơn giản, càng về sau càng trộn nhiều loại
Người chơi gặp từng loại mới khi đã quen loại cũ. Danh sách loại là của `feature-enemy`. Đã loại: mọi loại có ngay từ đợt đầu.

### scale-reward | AI 7 | Tiền khi hạ một con quái giữ nguyên qua các đợt; đợt sau cho nhiều tiền hơn vì đông quái hơn
Tiền mỗi con chỉ theo loại quái (`feature-enemy`). Thu nhập theo kịp giá nâng cấp nhờ số quái và loại quái đắt hơn mở dần (`feature-balance`). Đã loại: tiền mỗi con nhân theo đợt (hai nơi cùng tăng tiền, khó cân bằng).
