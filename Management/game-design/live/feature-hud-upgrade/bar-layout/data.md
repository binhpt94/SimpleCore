---
based-on: cdb98c5c2b2c
refs: feature-upgrade/upgrade-list@7294a54c79df, feature-hud-upgrade/button-state@f1aa8b544f79
---
# Bố cục thanh nâng cấp

Thanh nâng cấp là một dải nút cố định ở mép dưới màn hình trận, luôn hiện, chia hai nhóm: nút hero và nút thành. Mỗi mục nâng cấp có đúng một nút.

## Ý cốt lõi

### bar-bottom-edge | AI 7 | Thanh nằm cố định ở mép dưới màn hình, dưới dải chiến trường
Ngón cái chạm tới dễ khi cầm ngang; dải đất chạy ngang giữa màn hình nên mép dưới không che trận. Đã loại: mép trên (xa tay); hai mép bên (che chỗ quái vào).

### bar-one-button-per-item | AI 8 | Mỗi mục nâng cấp có đúng một nút, đủ nút cho mọi mục
Danh sách mục là của `feature-upgrade/upgrade-list`. Đã loại: một nút mở danh sách con.

### bar-two-groups | AI 7 | Nút hero xếp thành một nhóm, nút thành xếp thành nhóm riêng
Liếc là biết đang nâng hero hay nâng thành. Nút hero xếp theo thứ tự đội đã chọn. Đã loại: trộn chung.

### bar-always-visible | AI 8 | Thanh hiện suốt trận, không thu gọn, không bật tắt
Đã loại (vòng 2): bảng nâng cấp bật lên.
