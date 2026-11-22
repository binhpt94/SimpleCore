---
based-on: 1a74f8c24f86
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

### scale-reward | AI 6 | Tiền khi hạ một con quái tăng theo đợt
Để thu nhập theo kịp giá nâng cấp tăng theo cấp; mức tăng chậm hơn sức quái (`feature-balance`). Đã loại: tiền mỗi con cố định.
