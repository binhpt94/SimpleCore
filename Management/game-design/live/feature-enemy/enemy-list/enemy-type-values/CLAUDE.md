# Năm loại quái: hộp, kiểu đánh, đợt xuất hiện

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Cung cấp danh sách loại quái (mã, tên tạm, hộp, cận chiến hay bắn xa, đợt đầu có mặt) cho `feature-wave` và cho việc sinh quái.
- Hiện quái bằng sprite 2D gen bằng AI, vừa hộp của loại, quay mặt về phía thành.
- Chỉ số của từng loại ở `feature-enemy/enemy-stats/enemy-stat-values`; tiền ở `feature-enemy/enemy-reward/reward-values`.

## Thông số
| Loại | Mã | Hộp rộng×cao (u) | Kiểu đánh | Tầm (u) | Đợt đầu có mặt | Lí do |
|---|---|---|---|---|---|---|
| Lính | soldier | 1×1.2 | Cận chiến | 1.0 | 1 | Loại chuẩn, có ngay |
| Sói | wolf | 1.2×0.8 | Cận chiến | 1.0 | 3 | Nhanh, thử phản ứng khi người chơi đã quen |
| Khổng lồ | giant | 2×2.5 | Cận chiến | 1.2 | 5 | Trâu, cần hero một mục tiêu đã có cấp |
| Cung thủ quái | archer | 1×1.2 | Bắn xa | 5 | 7 | Ép đội có hero bắn xa |
| Trùm | boss | 3×3.5 | Cận chiến | 1.5 | 10, 20, 30... | Đỉnh căng thẳng mỗi 10 đợt |

Số liệu là dự kiến theo bộ số chung.

Hiệu ứng (mức tối thiểu, dùng chung với `feature-enemy/enemy-behavior/enemy-move-values`):
| Hiệu ứng | Xuất hiện | Biến mất |
|---|---|---|
| Hình đạn Cung thủ quái | mỗi đòn | tới mục tiêu sau 0.15s |
| Trùm vào màn: phóng to 1.2 lần rồi về | lúc tâm Trùm vào x=±16u | sau 0.5s |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Loại có trong game | Lính, Sói, Khổng lồ | đủ 5 loại | đủ 5 loại |
| Đợt đầu có mặt | Lính 1, Sói 3, Khổng lồ 5 | như bảng | theo số đã chỉnh sau chơi thử |
| Trùm | không có | 1 con mỗi đợt chia hết cho 10 | như PROTOTYPE |
| Hình | 1 sprite mỗi loại, anim bằng move/scale/nháy | như IDEA | anim đi, đánh, bị hạ riêng; âm thanh |

## Trường hợp biên
- Đợt chưa tới đợt đầu của một loại: loại đó không ra; chia số quái cho các loại đã có là việc của `feature-wave`.
- Trùm ở đợt 10 nhưng số quái sống đã chạm trần 40 (`feature-wave`): Trùm chờ tới khi có chỗ, vẫn là con cuối của đợt.
- Hai Trùm cùng lúc: chỉ có thể khi Trùm đợt 10 còn sống tới đợt 20; cho phép, không có luật riêng.
- Cung thủ quái đứng ở x=±7.0u đánh thành: trong tầm tự bắn và Sấm sét; hero cận chiến phải đi ra tới nó.

## Đã tự quyết
- **IDEA dùng Lính, Sói, Khổng lồ.** Ba loại khớp đợt 1, 3, 5, đủ thấy chuẩn, nhanh, trâu trong 5 đợt đầu. Đã loại: có Cung thủ quái ở IDEA (chỉ ra từ đợt 7, ít người chơi thử tới).
- **Trùm ra cuối đợt.** Người chơi đã dọn bớt quái thường rồi mới gặp Trùm. Đã loại: Trùm ra đầu đợt.

## Chưa rõ
- Thứ tự trong đợt và tỉ lệ loại: `feature-wave` quyết; node này chỉ ghi đợt đầu có mặt và Trùm ra cuối.
