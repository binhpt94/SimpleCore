---
based-on: 09ba60c454c5
refs: feature-castle/castle-skills@8bc45d0edc8d, feature-run/run-pause@f8e9d49f5079, feature-hud/hud-layout@e541be0cca19, glossary#castle-skill@1c6e2774b71d
---
# Nút kĩ năng thành

Mỗi kĩ năng của thành có một nút trên HUD. Nút cho thấy kĩ năng sẵn sàng hay đang hồi chiêu, và thời gian hồi chiêu còn lại.

## Ý cốt lõi

### skill-one-button-each | AI 8 | Mỗi kĩ năng của thành có đúng một nút, chạm một lần là dùng
Kĩ năng có những gì do `feature-castle` quyết; HUD tạo nút theo danh sách đó. Bấm là dùng ngay, không chọn vị trí (`feature-castle/castle-skills`).

### skill-two-states | AI 8 | Nút có hai trạng thái: sẵn sàng và đang hồi chiêu
Sẵn sàng thì sáng và bấm được; đang hồi chiêu thì mờ và bấm không có tác dụng.

### skill-cooldown-show | AI 8 | Khi đang hồi chiêu, nút hiện thời gian còn lại tới lúc dùng lại được
Kèm một lớp phủ vơi dần để liếc là thấy sắp xong chưa. Đã loại: chỉ mờ đi, không báo còn bao lâu.

### skill-ready-cue | AI 6 | Nút báo rõ lúc vừa hồi xong
Người chơi đang nhìn chiến trường vẫn nhận ra kĩ năng dùng được lại, ví dụ nút nảy lên hoặc loé sáng.

### skill-hud-only-relays | AI 8 | Nút chỉ báo "đã bấm" cho `feature-castle`; dùng được hay không và hồi chiêu do `feature-castle` quyết
