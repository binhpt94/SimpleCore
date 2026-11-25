# Năm loại quái: hộp và kiểu đánh

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ danh sách loại quái: mã, tên tạm, hộp, cận chiến hay bắn xa, tầm. Sinh một con khi `feature-wave` gửi yêu cầu thả kèm loại, bên và hệ số.
- Hiện quái bằng sprite 2D gen bằng AI, vừa hộp của loại, quay mặt về phía thành.
- Chỉ số của từng loại ở `feature-enemy/enemy-stats/enemy-stat-values`; tiền ở `feature-enemy/enemy-reward/reward-values`.
- Không quyết loại nào ra ở đợt nào: `feature-wave/difficulty-scaling/wave-formula` (mốc mở loại, chu kì Trùm) và `feature-wave/spawning/spawn-order` (Trùm ra cuối, trần quái sống).

## Thông số
| Loại | Mã | Hộp rộng×cao (u) | Kiểu đánh | Tầm (u) | Lí do |
|---|---|---|---|---|---|
| Lính | soldier | 1×1.2 | Cận chiến | 1.0 | Loại chuẩn |
| Sói | wolf | 1.2×0.8 | Cận chiến | 1.0 | Nhanh, thấp dài |
| Khổng lồ | giant | 2×2.5 | Cận chiến | 1.2 | Trâu, to hơn hẳn hero |
| Cung thủ quái | archer | 1×1.2 | Bắn xa | 5 | Ép đội có hero bắn xa |
| Trùm | boss | 3×3.5 | Cận chiến | 1.5 | Đỉnh căng thẳng |

Số liệu là dự kiến theo bộ số chung.

Hiệu ứng (mức tối thiểu, dùng chung với `feature-enemy/enemy-behavior/enemy-move-values`):
| Hiệu ứng | Xuất hiện | Biến mất |
|---|---|---|
| Hình đạn Cung thủ quái | mỗi đòn | tới mục tiêu sau 0.15s |
| Trùm vào màn: phóng to 1.2 lần rồi về | lúc tâm Trùm vào x=±16u | sau 0.5s |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Loại có trong game (có hình, sinh được) | Lính, Sói, Khổng lồ | đủ 5 loại | đủ 5 loại |
| Hình | 1 sprite mỗi loại, anim bằng move/scale/nháy | như IDEA | anim đi, đánh, bị hạ riêng; âm thanh |

## Trường hợp biên
- **Yêu cầu thả một loại chưa có ở giai đoạn đang làm** (ví dụ Cung thủ quái ở IDEA): lỗi nêu mã loại, không sinh; cấu hình của `feature-wave` sai.
- **Hai Trùm cùng lúc** (Trùm đợt 10 còn sống tới đợt 20): cho phép, mỗi con theo luật của mọi quái.
- **Cung thủ quái đứng ở x=±7.0u đánh thành**: trong tầm tự bắn và Sấm sét; hero cận chiến phải đi ra tới nó.

## Đã tự quyết
- **IDEA dùng Lính, Sói, Khổng lồ.** Ba loại khớp mốc mở loại đợt 1, 3, 5 của `feature-wave`, đủ thấy chuẩn, nhanh, trâu trong 5 đợt đầu. Đã loại: có Cung thủ quái ở IDEA (chỉ ra từ đợt 7, ít người chơi thử tới).
- **Bỏ luật đợt xuất hiện khỏi node này (2026-11-25, theo hướng A người đã chọn cho cùng kiểu lỗi, qua @Manager).** Bản trước có ý `type-first-wave` (đợt đầu có mặt), phần "đúng 1 con, ra cuối đợt" của `boss-single`, hai trường hợp biên (loại chưa tới đợt không ra; Trùm chờ khi chạm trần 40) và cột, hàng "Đợt đầu có mặt", "Trùm". Các luật đó đã có ở `wave-formula` (`type-unlock`, `boss-wave`) và `spawn-order`, và node cha ghi "loại nào ra ở đợt nào: `feature-wave`". Sổ yêu cầu vì vậy giao chúng cho Enemy dù quái không có code cho chúng. Phần còn lại của `boss-single` thành ý `boss-no-skill`. Đã loại: giữ bản chép (hai nơi cùng một luật, sẽ lệch).

## Chưa rõ
- Không có điểm chặn.
