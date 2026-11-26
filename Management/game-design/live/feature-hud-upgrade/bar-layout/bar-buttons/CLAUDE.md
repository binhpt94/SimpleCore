# Nút trên thanh nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Khi trận bắt đầu, nhận danh sách 6 mục theo thứ tự từ `feature-upgrade/upgrade-list/item-set` và tạo đúng 6 nút theo thứ tự đó.
- Xếp hai nhóm dọc theo mép dưới màn hình (`feature-hud-upgrade/bar-layout`):
  - Nhóm hero: `hero-1`, `hero-2`, `hero-3`.
  - Nhóm thành: `castle-health`, `castle-attack`, `castle-skill`.
- Giữa hai nhóm có khoảng trống. Chỗ cụ thể của từng nhóm trên mép dưới, cỡ nút: do người làm giao diện quyết.
- Thanh hiện suốt trận; ẩn khi popup kết thúc lượt hiện.
- Nội dung từng nút (hình, cấp, giá, sáng/mờ): `feature-hud-upgrade/button-state/button-display`.

Mỗi nút đại diện cho:
| Nút | Đại diện cho | Mua xong thì |
|---|---|---|
| Hero 1, 2, 3 | Hero ở chỗ đó trong đội | Hero đó lên một cấp: máu, sát thương, tốc độ đánh tăng |
| Máu | Máu thành | Máu tối đa và máu hiện tại của thành +150 |
| Sức đánh | Đòn tự bắn của thành | Sát thương mỗi phát tự bắn tăng |
| Kĩ năng | Cả ba kĩ năng thành (Sấm sét, Băng giá, Cổ vũ) | Cả ba kĩ năng mạnh hơn, hồi chiêu không đổi |

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số nút | 6 | nút | 3 hero + 3 thành (bộ số chung) |
| Số nhóm | 2 | nhóm | Vòng 3 |
| Khoảng trống giữa hai nhóm | rộng hơn khoảng cách giữa hai nút cùng nhóm | | Đủ thấy hai nhóm; độ rộng cụ thể là việc giao diện |
| Số lần thanh ẩn trong trận | 0 | lần | Luôn hiện (vòng 3) |
| Tỉ lệ màn hình phải kiểm | 16:9 và 20:9 | | Hai tỉ lệ điện thoại cầm ngang phổ biến |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Số nút | 6 | 6 | 6 |
| Tỉ lệ màn hình được kiểm | 16:9 | 16:9 và 20:9 | 16:9, 20:9 và 4:3 |
| Khoảng trống giữa hai nhóm | có | có | có, đủ art |
| Nhãn tên nhóm ("Hero", "Thành") | không | không | tuỳ người làm giao diện |

## Trường hợp biên
- **Đội không đủ 3 hero** (lỗi dữ liệu): `item-set` đã báo lỗi; thanh không tự tạo ít nút hơn.
- **Hero gục**: nút hero vẫn ở nguyên chỗ, vẫn bấm được nếu đủ tiền.
- **Lượt kết thúc**: thanh ẩn khi popup kết thúc lượt hiện; chạm trong khoảng giữa thành về 0 máu và lúc popup hiện không mua được (`purchase-rules`).
- **Lượt mới**: thanh tạo lại 6 nút theo đội mới.
- **Nghỉ giữa đợt**: thanh hiện, nút sáng/mờ như thường.

## Đã tự quyết
- **6 nút, không thêm nút phụ.** Đủ cho mọi mục, một chạm là mua. Đã loại: nút thu gọn thanh.
- **Hero trước, thành sau.** Khớp thứ tự `item-set`. Đã loại: thành trước.
- **Vị trí và cỡ nút chỉ ước lượng, trong frame `main.frame` (2026-11-23, luật frame mới).** Thanh x 40–1380 px ở dải dưới, cách cụm kĩ năng (x 1420) 40 px; nút 200x140 px (`use` từ `feature-hud/hud-layout/layout-spec`); nút cùng nhóm cách 10 px, hai nhóm cách 70 px nên `group-gap` thoả. Cỡ cuối cùng do người làm giao diện chỉnh.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ trường hợp biên tạm dừng.

## Chưa rõ
- `feature-hud` cần biết mép dưới đã do thanh nâng cấp giữ.
