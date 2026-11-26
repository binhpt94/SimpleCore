---
based-on: ddaad7454112
refs: feature-upgrade/upgrade-effect@b9bcec737959, feature-upgrade/upgrade-purchase@dfecac7ac2de, glossary#level@43a706597ab9
---
# Danh sách nâng cấp

Các mục nâng cấp người chơi mua được trong trận: mỗi hero trong đội một mục, thành ba mục. Danh sách cố định suốt lượt, mỗi mục có một cấp bắt đầu từ thấp nhất.

## Ý cốt lõi

### hero-level-item | AI 8 | Mỗi hero trong đội có đúng một mục "cấp hero"
Ít nút, dễ hiểu với người chơi phổ thông. Đã loại: mỗi chỉ số của hero một mục.

### castle-items | AI 8 | Thành có đúng ba mục: máu, sức đánh, kĩ năng
Khớp với `feature-castle` (`castle-upgrades`). Đã loại: gộp thành một mục "cấp thành" (mất lựa chọn giữa giữ thành và đánh mạnh).

### list-fixed | AI 8 | Danh sách mục cố định trong lượt, theo đội đã chọn
Đội có bao nhiêu hero thì có bấy nhiêu mục hero, cộng ba mục thành. Không có mục mới xuất hiện giữa trận. Đã loại (vòng 2): bốc thẻ ngẫu nhiên.

### item-level | AI 7 | Mỗi mục có một cấp riêng, vào lượt ở cấp thấp nhất, mỗi lần mua tăng một cấp
Cấp của các mục độc lập với nhau. Đã loại: một cấp chung cho cả đội.
