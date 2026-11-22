---
based-on: c5b904d13f66
---
# Hero vào trận, gục và hồi sinh

Vào trận cả đội sinh ra ở thành cùng lúc. Hero máu về 0 thì gục, rời trận, chờ một lúc rồi hồi sinh ở thành đầy máu, giữ nguyên cấp. Trong trận không có hero nào được thêm vào đội.

## Ý cốt lõi

### team-enter | AI 8 | Vào trận cả đội sinh ra ở thành cùng một lúc
Đội là đội đã chọn ở `feature-team-select`. Đã loại (người loại ở vòng 1): thành tự sinh hero theo thời gian.

### hero-down | AI 8 | Hero máu về 0 thì gục và rời chiến trường
Hero gục không đánh, không bị đánh, không chắn quái.

### respawn-wait | AI 7 | Hero gục chờ một khoảng thời gian rồi hồi sinh ở thành, đầy máu
Thời gian chờ như nhau với mọi hero và không đổi theo cấp. Đã loại: chờ lâu hơn khi cấp cao (phạt việc nâng hero), hồi sinh tại chỗ gục.

### respawn-keep-level | AI 8 | Gục không mất cấp, không mất tiền
Phạt của việc gục chỉ là đội thiếu người trong lúc chờ. Đã loại (vòng 2): mất cấp khi gục.

### no-self-heal | AI 7 | Hero không tự hồi máu trong trận; máu chỉ về đầy khi hồi sinh
Hero còn được hồi máu bằng kĩ năng Cổ vũ của thành (`feature-castle/castle-skills`). Đã loại: tự hồi theo thời gian, hồi đầy khi dọn sạch đợt.
