# Chỗ của từng phần HUD

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Đặt từng phần HUD theo bảng dưới, bám mép và góc màn hình (không bám toạ độ cố định) để chạy đúng ở nhiều tỉ lệ màn hình.
- Chừa phần mép dưới bên trái cho `feature-hud-upgrade`.
- Đặt mọi phần trong vùng an toàn của máy.

## Thông số
| Phần | Mép / góc | Thứ tự | Lí do |
|---|---|---|---|
| Nút tạm dừng | Góc trên bên trái | 1 (trái nhất) | Xa cụm kĩ năng (góc dưới phải) và đầu thanh nâng cấp |
| Tiền | Mép trên, bên trái máu | 2 | Gần thanh nâng cấp hơn để liếc tiền rồi mua |
| Máu thành | Giữa mép trên | 3 | Thẳng phía trên thành |
| Đợt | Mép trên, bên phải | 4 (phải nhất) | |
| Thông báo "Đợt N" | Mép trên, ngay dưới máu thành | | Thoáng qua 1.5 giây, không che dải chiến trường |
| 3 nút kĩ năng | Góc dưới bên phải, xếp ngang | Sấm sét, Băng giá, Cổ vũ | Ngón cái phải |
| Thanh nâng cấp | Mép dưới, từ cạnh trái tới sát cụm kĩ năng | | `feature-hud-upgrade/bar-layout` |

| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Tỉ lệ màn hình hỗ trợ | 16:9 tới 21:9, cầm ngang | | Điện thoại phổ biến |
| Hướng màn hình | chỉ ngang | | Node gốc |
| Số phần HUD giao với dải chiến trường | 0 | | `layout-clear-strip` |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Phần có mặt | Máu, tiền, đợt, 3 nút kĩ năng | Thêm thông báo đợt (nút tạm dừng không bắt buộc) | Đủ, có nút tạm dừng |
| Tỉ lệ màn hình phải kiểm | 16:9 | 16:9 và 20:9 | 16:9 tới 21:9 |
| Vùng an toàn | Không bắt buộc | Có | Có |

## Trường hợp biên
- **Màn hình rất rộng (21:9):** mép trên giãn ra; tiền và đợt bám về phía máu hay về hai mép là việc của phiên code, miễn thứ tự giữ nguyên và không che dải chiến trường.
- **Thanh nâng cấp cần nhiều chỗ hơn (đội 3 hero + 3 nút thành = 6 nút):** thanh tự co nút trong phần của nó; không lấn sang cụm kĩ năng.
- **Popup mở:** popup che lên mọi phần HUD (`feature-popup/popup-layer`).
- **Thông báo "Đợt N":** nằm trong mép trên, ngay dưới máu thành, không lấn xuống dải chiến trường; không chặn chạm, tự tắt sau 1.5 giây (`feature-hud/hud-status/status-spec`).

## Đã tự quyết
- **Kĩ năng góc dưới bên phải, thanh nâng cấp phía trái.** Trả lời "Chưa rõ" của node cha. Phần đông người chơi thuận tay phải; kĩ năng là thao tác cần canh đúng lúc nên giao cho ngón cái phải. Đã loại: kĩ năng bên trái.
- **Nút tạm dừng góc trên bên trái.** Chéo hẳn với cụm kĩ năng, khác mép với thanh nâng cấp.
- **Kích thước chỉ ước lượng, nằm trong frame `main.frame` (2026-11-23, luật frame mới).** Trên 1920x1080: dải trên cao 140 px, dải dưới cao 180 px, dải chiến trường ở giữa (y 140–900) không HUD nào chạm. Cạnh nút chạm tối thiểu 120 px (trên ngưỡng 96 px), lề trái phải 40 px, nút kĩ năng 140 px, nút nâng cấp 200x140 px. Các số này đặt bằng `let` ở đây để các node HUD và popup khác `use`, không chép lại. Kích thước cuối cùng vẫn do chuyên gia giao diện chỉnh. Đã loại: mỗi node tự đặt cỡ nút (dễ lệch).
- **Thông báo "Đợt N" nằm trong dải trên, dưới thanh máu** (y 76–132), vì dải trên 140 px đủ chỗ cho cả hai mà không lấn chiến trường.

## Chưa rõ
- `feature-hud-upgrade` cần đồng ý dùng phần mép dưới bên trái, chừa góc dưới phải cho cụm kĩ năng.
