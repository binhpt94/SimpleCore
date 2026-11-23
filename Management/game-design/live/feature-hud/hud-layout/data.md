---
based-on: 09ba60c454c5
refs: feature-hud/hud-status@5e6dc1e8532e, feature-hud/hud-skill-button@dda5759077eb, feature-hud/hud-pause-button@4ab96ef15d1f
---
# Bố cục HUD

HUD dùng mép trên và một góc dưới của màn hình; dải chiến trường ở giữa luôn để trống. Mép dưới còn lại để cho thanh nâng cấp (`feature-hud-upgrade`).

## Ý cốt lõi

### layout-top-info | AI 7 | Máu thành, tiền và số đợt nằm ở mép trên màn hình
Máu thành ở giữa mép trên, thẳng phía trên thành, để nhìn thành là thấy máu. Tiền và số đợt ở hai bên. Đã loại: thanh máu đặt ngay trên đầu thành trong chiến trường (che quái đứng gần thành).

### layout-pause-corner | AI 6 | Nút tạm dừng ở một góc trên, xa các nút bấm nhiều

### layout-skill-corner | AI 7 | Nút kĩ năng thành gom ở một góc dưới, chỗ ngón cái chạm tới
Cầm ngang điện thoại thì ngón cái chạm góc dưới dễ nhất. Đã loại: nút kĩ năng ở mép trên (với tay xa).

### layout-share-bottom | AI 7 | Mép dưới chia với thanh nâng cấp: HUD giữ một góc, phần còn lại của thanh nâng cấp
Hai feature không chồng lên nhau. Thanh nâng cấp tự quyết cách xếp trong phần của nó (`feature-hud-upgrade`).

### layout-clear-strip | AI 8 | Không phần nào của HUD nằm trên dải chiến trường hai bên thành
Đã loại: HUD nổi giữa màn hình.
