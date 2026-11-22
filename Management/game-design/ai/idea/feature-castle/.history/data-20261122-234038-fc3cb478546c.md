---
based-on: cf568e2870fe
---
# Thành

Tòa thành đứng giữa màn hình: thứ người chơi phải bảo vệ, nơi đội hero xuất phát và hồi sinh. Thành tự bắn quái trong tầm và có kĩ năng người chơi bấm để dùng; máu về 0 là thua.

## Ý cốt lõi

### castle-health | AI 8
Thành có máu tối đa và máu hiện tại. Quái tới được thành thì đánh trừ máu. Máu về 0 thì báo thua cho `feature-run`.

### castle-no-regen | AI 6
Máu thành không tự hồi giữa các đợt. Nâng cấp máu thì cả máu tối đa và máu hiện tại cùng tăng thêm phần đó.

### castle-auto-attack | AI 7
Thành tự bắn quái gần nhất trong tầm, cả hai phía, không cần người chơi bấm.

### castle-skills | AI 7
Thành có kĩ năng (dự kiến 2 cho bản đầu), mỗi kĩ năng một nút trên màn hình. Bấm thì dùng ngay, sau đó chờ hết thời gian hồi chiêu mới dùng lại; nút hiện rõ thời gian còn lại.

### castle-hero-base | AI 7
Thành là nơi đội hero xuất hiện khi vào trận và là nơi hero gục hồi sinh.

### castle-upgrades | AI 7
Ba thứ của thành nâng cấp được: máu, sức đánh (của đòn tự bắn), kĩ năng (mạnh hơn hoặc hồi chiêu ngắn hơn).
