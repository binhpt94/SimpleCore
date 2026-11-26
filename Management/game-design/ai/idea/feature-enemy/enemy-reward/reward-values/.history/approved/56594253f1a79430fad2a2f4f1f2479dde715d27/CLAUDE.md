# Mức tiền khi hạ quái

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ bảng tiền theo loại quái (asset cấu hình, không viết cứng).
- Khi `feature-enemy/enemy-stats/enemy-stat-values` báo một quái bị hạ: cộng tiền của loại đó vào `feature-wallet` ngay, một lần.
- Hiện chữ số tiền nhận ở chỗ quái bị hạ.

## Thông số
| Loại | Tiền | Máu đợt 1 | Tiền / 10 máu | Lí do |
|---|---|---|---|---|
| Lính | 5 | 40 | 1.25 | Mức chuẩn |
| Sói | 4 | 20 | 2.0 | Ít hơn Lính (luật vòng 3) |
| Khổng lồ | 15 | 150 | 1.0 | Nhiều hơn Lính |
| Cung thủ quái | 6 | 25 | 2.4 | Ngang Lính, hơi hơn vì khó với tới |
| Trùm | 100 | 400 | 2.5 | Nhiều nhất; một lần mua đáng kể, là phần thưởng riêng của đợt Trùm |

| Thông số khác | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Nhân theo đợt | không | | Bộ số chung |
| Chữ tiền bay lên | 1u trong 0.6s | u, s | Đủ đọc, không che trận |

Cân bằng dự kiến (chỉ tính tiền quái, chưa cộng thưởng dọn đợt 20+5×đợt của `feature-wave`):
- Đợt 1: 8 Lính = 40.
- Đợt 5: 16 quái, giả sử 8 Lính, 5 Sói, 3 Khổng lồ = 40+20+45 = 105.
- Đợt 10: 26 quái gồm 1 Trùm, giả sử 10 Lính, 7 Sói, 5 Khổng lồ, 3 Cung thủ quái = 50+28+75+18+100 = 271.
Giá cấp hero thứ 5 là 30×1.35^4 ≈ 100: tới đợt 10 người chơi mua được khoảng 2-3 cấp mỗi đợt.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Loại có tiền | Lính 5, Sói 4, Khổng lồ 15 | cả 5 loại như bảng | theo số đã chỉnh sau chơi thử |
| Chữ tiền bay lên | không bắt buộc | có, 1u trong 0.6s | có, kèm âm thanh |

## Trường hợp biên
- Hai đòn cùng khung hình hạ một quái (ví dụ Sấm sét và một mũi tên): tiền một lần.
- Đòn vùng hạ nhiều quái: mỗi quái cho tiền riêng, cùng khung hình.
- Quái bị hạ cùng khung hình thành bị phá: thành bị phá xử lý trước, không cộng tiền (`feature-run` đã dừng lượt).
- Quái bị hạ lúc nghỉ giữa hai đợt: vẫn cho tiền.
- Ví có giới hạn hay không: việc của `feature-wallet`; node này luôn gửi đủ số tiền.

## Đã tự quyết
- **Thành bị phá trước, tiền sau trong cùng khung hình.** Lượt đã thua thì tiền vô nghĩa và tránh `feature-wallet` nhận tiền sau khi lượt kết thúc.
- **Cột máu Trùm theo máu gốc mới 400 (2026-11-23).** Tiền Trùm giữ 100; chỉ cập nhật cột tham khảo theo `feature-enemy/enemy-stats/enemy-stat-values`.
- **Chữ tiền là của node này, không của HUD.** Gắn với chỗ quái bị hạ trên chiến trường, không phải thanh trên màn hình.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ trường hợp biên tạm dừng.

## Chưa rõ
- Không có điểm chặn.
