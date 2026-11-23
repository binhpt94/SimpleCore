# Thông số đi và đánh của quái

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
Mỗi khung hình, với mỗi quái còn sống:
1. Tìm hero đang đứng (không gục, không trong 0.3s hồi sinh) có khoảng cách ≤ tầm của quái. Có: **Đánh hero** gần nhất (đang đánh hero nào thì giữ hero đó nếu nó còn trong tầm).
2. Không có hero: khoảng cách tới mép thành ≤ tầm thì **Đánh thành**.
3. Không thì **Đi** về phía thành theo tốc độ đi hiện tại; không đi vào trong thành.
- Ra đòn theo nhịp và sát thương ở `feature-hero/hero-stats/hero-level-values` (dùng chung); chỉ số của loại ở `feature-enemy/enemy-stats/enemy-stat-values`.
- Nhận trạng thái chậm từ `feature-castle/castle-skills/skill-values`.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Điểm sinh | x=-17 (bên trái), x=+17 (bên phải) | u | Ngay ngoài màn (màn tới ±16u) |
| Hướng đi | luôn về phía x=0 | | Một làn, không quay đầu |
| Tốc độ đi | theo loại, 0.6-3.0 | u/s | `feature-enemy/enemy-stats/enemy-stat-values` |
| Điểm dừng trước thành | mép quái cách mép thành đúng bằng tầm | u | Không đi sát hơn cần thiết |
| Chậm do Băng giá | tốc độ đi ×0.5, giây mỗi đòn ×2 | | Bộ số chung (chậm 50%) |
| Quái chắn nhau | không | | Luật vòng 3; đi xuyên qua nhau |

Điểm dừng trước thành theo loại (tâm quái, bên phải; bên trái là số âm):
| Loại | Tầm (u) | Rộng (u) | Tâm dừng x (u) |
|---|---|---|---|
| Lính | 1.0 | 1 | 3.0 |
| Sói | 1.0 | 1.2 | 3.1 |
| Khổng lồ | 1.2 | 2 | 3.7 |
| Cung thủ quái | 5 | 1 | 7.0 |
| Trùm | 1.5 | 3 | 4.5 |

Mọi điểm dừng nằm trong tầm tự bắn và vùng Sấm sét (mép trong ≤ 7.5u).

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Luật đi, dừng, đánh | như bảng | như bảng | như bảng |
| Chậm do Băng giá | không có (IDEA chưa có Băng giá) | ×0.5 đi, ×2 giây mỗi đòn | theo số đã chỉnh |
| Chuyển động | nhún khi đi, nhích khi đánh, nháy khi bị đánh | như IDEA | anim đi, đánh riêng từng loại |
| Dấu chậm | không có | hiện suốt thời gian chậm | hiệu ứng riêng |

## Trường hợp biên
- Hero đi xuyên qua quái và ra phía sau (phía mép màn): khoảng cách vẫn đo được; còn trong tầm thì quái vẫn đánh, quái không quay đầu đi theo.
- Hero đang bị đánh gục: quái xét lại ngay khung hình đó; có hero khác trong tầm thì đánh hero đó, không thì đi tiếp hoặc đánh thành.
- Quái đứng đánh thành, một hero hồi sinh ngay sát nó: sau 0.3s hero hiện đủ thì quái chuyển sang đánh hero.
- Quái đang đứng đánh hero mà hero vào trạng thái gục: lần chờ đòn không đặt lại.
- Nhiều quái chồng hình nhau: cho phép, không đẩy nhau ra.
- Băng giá trúng quái đang đứng đánh: nó vẫn đứng đánh, chỉ chậm nhịp đánh.
- Thành bị phá: quái đứng yên tới khi `feature-run` dừng trận.
- Tạm dừng: quái đứng yên, giữ trạng thái, thời gian chờ đòn và thời gian chậm.

## Đã tự quyết
- **Quái giữ hero đang đánh nếu còn trong tầm.** Tránh đổi mục tiêu liên tục khi nhiều hero đứng gần nhau. Đã loại: mỗi khung hình chọn lại hero gần nhất.
- **Dừng ngay mép tầm trước thành.** Một luật cho cả quái cận chiến và bắn xa.
- **Trúng chậm giữa nhịp đánh thì giãn phần còn lại.** Tác dụng chậm thấy ngay, không đợi tới đòn sau.

- **Frame `stop-points.frame` (2026-11-23).** Hộp, tầm của từng loại `use` từ `feature-enemy/enemy-list/enemy-type-values`; điểm ra `use` từ `feature-wave/spawning/spawn-order`; thành và tầm tự bắn `use` từ `feature-castle`. Số tính ra: mép trong của quái dừng = mép thành 1.5u + tầm (Lính, Sói 2.5u; Khổng lồ 2.7u; Cung thủ quái 6.5u; Trùm 3.0u); tâm = mép trong + nửa rộng, khớp bảng "Điểm dừng trước thành". Mỗi loại vẽ một hàng chỉ để dễ đọc.

## Chưa rõ
- Không có điểm chặn.
