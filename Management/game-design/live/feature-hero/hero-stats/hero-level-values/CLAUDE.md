# Cách tính chỉ số và cấp hero

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Định nghĩa cách đo khoảng cách, nhịp đánh, sát thương cho cả game. Hero, quái (`feature-enemy/enemy-stats/enemy-stat-values`) và thành (`feature-castle/castle-attack/auto-shot-values`) cùng theo node này.
- Giữ cấp của từng hero trong đội (bắt đầu lượt ở cấp 1) và tính chỉ số theo cấp.
- Khi `feature-upgrade` báo mua cấp cho một hero: tăng cấp 1, tính lại chỉ số ngay.
- Chỉ số lv1 của từng hero ở `feature-hero/hero-list/hero-roster-values`.

## Thông số
| Thông số | Công thức / giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Khoảng cách | khoảng trống trục x giữa hai mép hộp; chồng nhau = 0 | u | Unit to nhỏ khác nhau (0.8u tới 3.5u) |
| Máu tối đa cấp n | lv1 × (1 + 0.15×(n-1)) | máu | Bộ số chung |
| Sát thương cấp n | lv1 × (1 + 0.15×(n-1)) | máu/đòn | Bộ số chung |
| Tăng tốc độ đánh cấp n | min(0.03×(n-1), 0.6) | tỉ lệ | Bộ số chung; trần ở cấp 21 |
| Giây mỗi đòn cấp n | lv1 / (1 + tăng tốc độ đánh) | s | Tốc độ đánh tăng thì thời gian chờ giảm |
| Tầm, tốc độ chạy | không đổi theo cấp | u, u/s | Luật vòng 3 |
| Hình đạn bay (đòn xa) | 0.15 | s | Chỉ để nhìn |
| Làm tròn hiển thị | máu làm tròn lên, sát thương làm tròn xuống | | Không hiện 0 máu khi chưa gục |

Ví dụ (dự kiến):
| Hero | Cấp 1 | Cấp 5 | Cấp 10 |
|---|---|---|---|
| Hiệp sĩ máu / sát thương / s mỗi đòn | 300 / 12 / 1.0 | 480 / 19.2 / 0.893 | 705 / 28.2 / 0.787 |
| Cung thủ | 120 / 14 / 0.8 | 192 / 22.4 / 0.714 | 282 / 32.9 / 0.630 |

Cân bằng dự kiến: Lính đợt n có 40×(1+0.1×(n-1)) máu, tăng 10%/đợt; hero tăng 15% lv1 mỗi cấp. Mua khoảng 2 cấp cho cả đội mỗi 3 đợt là theo kịp.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Máu, sát thương mỗi cấp | +15% lv1 | +15% lv1 | theo số đã chỉnh sau chơi thử |
| Tốc độ đánh mỗi cấp | +3%, trần 60% | +3%, trần 60% | theo số đã chỉnh |
| Phản hồi lên cấp | hero phóng to 1.2 lần rồi về trong 0.3s | như IDEA, hiện số cấp mới 1s | hiệu ứng và âm thanh riêng |
| Hình đạn đòn xa | 1 sprite bay thẳng 0.15s | như IDEA | hiệu ứng riêng từng hero |

## Trường hợp biên
- Hai unit chồng hình: khoảng cách 0, luôn trong tầm của nhau.
- Lên cấp trong lúc đang chờ đòn: thời gian chờ còn lại không đổi; đòn sau dùng nhịp mới.
- Lên cấp khi hero đang gục: chỉ tăng cấp; lúc hồi sinh lấy máu tối đa của cấp mới (`feature-hero/hero-respawn/respawn-values`).
- Mua nhiều cấp liên tiếp trong một khung hình: áp từng cấp một, kết quả như mua lần lượt.
- Sát thương làm máu xuống dưới 0: máu coi là 0; phần thừa không chuyển sang mục tiêu khác.
- Không có cấp tối đa; tốc độ đánh dừng tăng ở cấp 21, máu và sát thương vẫn tăng.

## Đã tự quyết
- **Đo bằng khoảng trống giữa mép hộp.** Một cách đo cho mọi tầm, khớp "tầm tính từ mép thành". Đã loại: đo từ tâm.
- **Sát thương tức thì cho mọi đòn.** Bớt trường hợp biên đạn bay; nhìn vẫn có đạn. Đã loại: đạn bay thật.
- **Số thực bên trong, làm tròn khi hiện.** Công thức +15% ra số lẻ (12×1.15=13.8); làm tròn từng cấp sẽ làm cấp thấp tăng không đều.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ trường hợp biên tạm dừng.

## Chưa rõ
- Không có điểm chặn.
