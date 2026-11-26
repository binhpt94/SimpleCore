---
based-on: 6da99ea96860
refs: feature-run/run-start@31cb7598b529, feature-run/run-flow@45cc90c1501f, feature-team-select/team-memory@4a24db16dcc6, feature-team-select/hero-cards@5fe7af230b3e
---
# Chỗ trong đội và luật chọn

Đội có một số chỗ cố định, ít hơn số hero trong danh sách. Người chơi chạm hero để đưa vào hoặc bỏ ra khỏi đội; đủ chỗ mới vào trận được.

## Ý cốt lõi

### slots-fixed | AI 8 | Đội có số chỗ cố định, ít hơn số hero trong danh sách
Phải bỏ lại một số hero mỗi lượt, đó là quyết định trước trận. Số chỗ do `feature-balance` chỉnh. Đã loại: đội lớn tuỳ ý.

### slots-unique | AI 8 | Mỗi hero chỉ vào đội một lần, không chọn trùng
Đội khác nhau là ghép cách đánh khác nhau; chọn trùng làm mất ý nghĩa danh sách. Đã loại: chọn trùng một hero nhiều lần.

### slots-toggle | AI 7 | Chạm hero trong danh sách để đưa vào chỗ trống; chạm hero trong đội để bỏ ra
Một chạm cho mỗi việc, hợp điện thoại. Đã loại: kéo thả vào từng chỗ.

### slots-full-to-start | AI 8 | Nút vào trận chỉ bấm được khi mọi chỗ trong đội đã có hero
Đã loại: vào trận với đội thiếu người.

### slots-order-free | AI 6 | Thứ tự hero trong đội không ảnh hưởng trận
Hero tự đánh theo cách đánh của mình (`feature-hero`); chỗ nào trong đội cũng như nhau. Đã loại: chỗ trước, chỗ sau quyết định vị trí đứng.
