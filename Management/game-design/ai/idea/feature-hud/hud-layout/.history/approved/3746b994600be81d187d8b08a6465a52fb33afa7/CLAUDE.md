# Bố cục HUD

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- HUD gồm các phần ở `feature-hud/hud-status`, `feature-hud/hud-skill-button`.
- `feature-hud-upgrade` dùng phần mép dưới không thuộc HUD; hai bên chia mép, không chồng.
- Dải chiến trường (vòng 1: dải đất chạy ngang qua thành) nằm giữa mép trên và mép dưới.
- Popup (`feature-popup`) che lên trên HUD khi mở.

## Định nghĩa
- `glossary#battlefield`

## Danh sách
| Phần | Chỗ |
|---|---|
| Thanh máu thành | Giữa mép trên |
| Tiền | Mép trên, một bên |
| Số đợt | Mép trên, bên còn lại |
| Nút kĩ năng thành | Một góc dưới |
| Thanh nâng cấp (`feature-hud-upgrade`) | Phần còn lại của mép dưới |

## Luật cơ bản
- Mọi phần HUD nằm trong mép trên hoặc góc dưới của HUD.
- Không phần nào che dải chiến trường.

## Đã tự quyết
- **Thông tin ở trên, nút bấm nhiều ở dưới.** Mắt nhìn trên, tay bấm dưới; hợp điện thoại cầm ngang.
- **Máu thành ở giữa, trên thành.** Thành đứng giữa màn hình (vòng 1).
- **Chia mép dưới với thanh nâng cấp.** Trả lời "Chưa rõ" vị trí của `feature-hud`; thanh nâng cấp cần nhiều nút nên lấy phần lớn mép dưới.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ nút tạm dừng (`hud-pause-button`) khỏi các phần HUD và luật đặt chỗ.

## Chưa rõ
- Góc trái hay góc phải cho nút kĩ năng, và phần mép dưới dành cho thanh nâng cấp, cần thống nhất với `feature-hud-upgrade`.
