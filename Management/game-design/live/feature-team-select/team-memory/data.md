---
based-on: fe94740dab29
refs: feature-run/run-score@6f1909d90f98, feature-team-select/team-slots@c34032308bdd, feature-run/run-start@31cb7598b529, feature-run/run-flow@db634f5dac0b
---
# Nhớ đội lượt trước

Màn chọn đội mở ra với đội của lượt vừa chơi đã xếp sẵn, nên ai muốn giữ đội chỉ cần bấm vào trận.

## Ý cốt lõi

### memory-prefill | AI 7 | Mở màn chọn đội thì đội của lượt trước đã nằm sẵn trong các chỗ
Người chơi vẫn đổi được như bình thường. Đã loại: lần nào cũng chọn lại từ đầu.

### memory-which-team | AI 7 | Đội được nhớ là đội đã vào trận gần nhất
Đội được nhớ luôn là một đội đủ đã từng đánh.

### memory-session-only | AI 6 | Đội chỉ được nhớ trong lúc game đang mở, tắt game thì quên
Kỉ lục là thứ duy nhất lưu qua khi tắt game (`feature-run/run-score`). Đội nhớ chỉ là lựa chọn sẵn cho tiện, không phải trạng thái của lượt. Đã loại: lưu đội khi tắt game (thành thêm một thứ lưu lâu dài).
