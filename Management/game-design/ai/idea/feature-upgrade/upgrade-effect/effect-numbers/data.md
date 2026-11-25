---
based-on: b9bcec737959
refs: feature-hero/hero-stats/hero-level-values@2d489af38e03, feature-castle/castle-upgrade-effects/upgrade-effect-values@b16439440823, feature-upgrade/upgrade-purchase/purchase-rules@ba593a5e3e09
frame: none
---
# Báo lên cấp cho bên giữ chỉ số

Mua xong một cấp thì nâng cấp báo "mục X lên cấp L" cho đúng bên giữ chỉ số, ngay trong khung hình mua. Mỗi cấp làm hero hay thành mạnh thêm bao nhiêu là của nhánh hero và nhánh thành.

## Ý cốt lõi

### numbers-at-owner | AI 8 | Nâng cấp không tính chỉ số; mức tăng mỗi cấp do bên giữ chỉ số quyết
Hero: `feature-hero/hero-stats/hero-level-values`. Thành (máu, sức đánh, kĩ năng): `feature-castle/castle-upgrade-effects/upgrade-effect-values`. Đã loại: nâng cấp giữ bảng mức tăng của mọi mục.

### report-same-frame | AI 8 | Mua thành công một cấp thì báo "mục X lên cấp L" trong cùng khung hình mua
Bên nhận áp hiệu lực ngay trong khung hình đó, nên người chơi thấy mạnh lên liền.

### report-right-owner | AI 8 | Mục hero báo cho `feature-hero` kèm đúng hero được mua; mục máu, sức đánh, kĩ năng thành báo cho `feature-castle`
Mỗi lần mua chỉ báo cho một bên, một mục.

### report-each-level | AI 7 | Mua nhiều cấp liên tiếp trong một khung hình thì báo từng cấp một theo thứ tự mua, mỗi lần tăng đúng 1
Kết quả như mua lần lượt ở các khung hình khác nhau (`feature-upgrade/upgrade-purchase/purchase-rules`: mỗi chạm một yêu cầu, xử lí lần lượt).

### report-only-up | AI 7 | Cấp được báo luôn lớn hơn cấp trước đúng 1; nâng cấp không bao giờ báo cấp thấp hơn hay báo lại cấp cũ
Không có hạ cấp, không có hoàn tiền.
