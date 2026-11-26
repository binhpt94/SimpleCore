## Phần tử
- top | khung | phụ | mép trên của HUD, chứa tiền, máu thành, đợt và thông báo đợt
- bottom | khung | phụ | mép dưới của HUD, chứa thanh nâng cấp và cụm kĩ năng
- strip | khung | phụ | dải chiến trường giữa hai mép, không phần HUD nào chồng lên
- money | nhãn | chính | tiền hiện có, chi tiết ở `feature-hud/hud-status/status-spec`
- hp | thanh | chính | máu thành, chi tiết ở `feature-hud/hud-status/status-spec`
- wave | nhãn | phụ | đợt đang đánh hoặc sắp tới
- banner | nhãn | phụ | thông báo "Đợt N" đầu đợt | trạng thái: ẩn, đang hiện
- skills | khung | chính | cụm 3 nút kĩ năng thành, chi tiết ở `feature-hud/hud-skill-button/skill-button-spec`
- upgrade | khung | chính | chỗ dành cho thanh nâng cấp của `feature-hud-upgrade`

## Tương tác
- Node này chỉ chia chỗ; mọi thao tác thuộc node sở hữu từng phần (nút kĩ năng, nút nâng cấp).
- money, hp, wave, banner: không nhận chạm; chạm vào banner đi xuyên xuống phần bên dưới.

## Flow
- Trận dựng xong → HUD trận hiện đủ các phần, giữ tới khi trận bị huỷ.
- HUD trận → popup thua chồng lên trên mọi phần HUD (`feature-popup/popup-layer`); popup đóng thì HUD dùng lại được.
