---
based-on: c34032308bdd
refs: feature-team-select/team-memory/memory-rules@e36a9ec522a1, feature-team-select/hero-cards/card-spec@786f5ada38ab, feature-run/run-start@d386ccae4d88, feature-hero/hero-list@1bf8998ab5a4, feature-hud/hud-layout/layout-spec@1a7ddf5f3b6c
---
# Ba chỗ trong đội và nút vào trận

Đội có 3 chỗ, chọn từ 5 hero, không trùng. Một chạm để thêm hoặc bỏ; nút "Vào trận" chỉ bấm được khi đủ 3.

## Ý cốt lõi

### slots-three | AI 8 | Đội có đúng 3 chỗ, chọn từ 5 hero; mỗi lượt bỏ lại 2 hero

### tap-add-first-empty | AI 7 | Chạm thẻ hero chưa chọn khi còn chỗ trống: hero vào chỗ trống có số nhỏ nhất
Bỏ hero ở chỗ 2 rồi chọn hero khác thì hero mới vào chỗ 2.

### tap-remove | AI 7 | Chạm thẻ hero đã chọn, hoặc chạm chỗ đang có hero: hero rời đội, chỗ đó trống
Các hero khác giữ nguyên chỗ, không dồn lên.

### tap-full-ignored | AI 7 | Đội đã đủ 3 mà chạm thẻ hero chưa chọn thì không có gì đổi
Phải bỏ một hero trước.

### start-button-gate | AI 8 | Nút "Vào trận" chỉ có tác dụng khi đủ 3 chỗ; thiếu thì nút ở trạng thái khoá, chạm không làm gì
Trạng thái khoá phải nhìn khác trạng thái bấm được.

### start-button-once | AI 7 | Chạm "Vào trận" khi đủ 3: báo đội cho `feature-run` đúng một lần, rồi màn không nhận chạm nữa

### screen-parts | AI 7 | Màn chọn đội có 3 phần: 5 thẻ hero, 3 chỗ trong đội, 1 nút "Vào trận"
Không có nút quay lại màn bắt đầu.
