# Thông số vào trận, gục và hồi sinh

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Lúc trận bắt đầu: nhận đội 3 hero theo thứ tự chỗ từ `feature-team-select`; sinh mỗi hero ở điểm xuất phát của chỗ đó, đầy máu, cấp 1.
- Mỗi khung hình: hero nào máu ≤ 0 thì chuyển sang gục, bắt đầu đếm 8s.
- Hết 8s: hồi sinh ở điểm xuất phát của chỗ đó, máu = máu tối đa của cấp hiện tại.
- Báo cho `feature-hud` trạng thái từng hero (đang đứng, đang gục và số giây còn lại) để hiện nếu HUD cần.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số hero sinh lúc vào trận | 3 | hero | Đội 3 chỗ |
| Điểm xuất phát chỗ 1 / 2 / 3 | -1.5 / +1.5 / -1.5 | x (u) | Mép thành, theo `feature-castle/castle-core/castle-body-values` |
| Thời gian chờ hồi sinh | 8 | s | Bộ số chung |
| Thu nhỏ khi gục | 0.3 | s | Thấy được lúc gục |
| Phóng to khi hồi sinh (không đánh, không bị đánh) | 0.3 | s | Tránh gục lại ngay |
| Máu khi hồi sinh | 100% máu tối đa cấp hiện tại | máu | Luật vòng 3 |

Cân bằng dự kiến: đợt 1 có 8 Lính thả mỗi 1.0s, đi 14.5u ở 1.5u/s mất khoảng 10s; một đợt đầu kéo dài khoảng 25-35s. Một hero gục mất 8s, đội còn 2/3 sức đánh trong khoảng 1/4 đợt.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Thời gian chờ hồi sinh | 8s | 8s (chỉnh sau chơi thử) | theo số đã chỉnh |
| Gục | thu nhỏ 0.3s | như IDEA | anim gục riêng, âm thanh |
| Hồi sinh | phóng to 0.3s | như IDEA | hiệu ứng hồi sinh riêng |
| Hiện thời gian chờ còn lại | không bắt buộc | có (qua `feature-hud`) | có |

## Trường hợp biên
- Cả 3 hero cùng gục: trận vẫn tiếp tục, chỉ thành bắn và kĩ năng; không thua (chỉ thành bị phá mới thua).
- Mua cấp cho hero đang gục: cấp tăng ngay; hồi sinh với máu tối đa của cấp mới; 8s chờ không đổi.
- Cổ vũ bấm khi hero đang gục: hero đó không được hồi, thời gian chờ không ngắn đi.
- Hero gục cùng khung hình với thành bị phá: không cần xử lý hồi sinh; `feature-run` dừng trận.
- Hero gục đúng lúc đòn của nó đang ra: đòn đó không ra (gục xử lý trước khi ra đòn trong khung hình).
- Hồi sinh khi đang nghỉ giữa hai đợt: hồi sinh bình thường, đứng chờ ở điểm xuất phát.
- Tạm dừng: đồng hồ 8s và anim 0.3s đứng yên.
- Hết lượt (`feature-run`): mọi hero bị gỡ, không hồi sinh; lượt mới sinh lại ở cấp 1.

## Đã tự quyết
- **Hồi sinh ở đúng điểm xuất phát của chỗ mình.** Một quy tắc cố định. Đã loại: hồi sinh về phía có quái gần nhất (thêm luật, không khác nhiều vì hero tự đi ra ngay).
- **0.3s không đánh, không bị đánh khi hồi sinh.** Thêm vì quái đánh thành đứng ngay mép thành, trùng điểm hồi sinh. Đã loại: không có thời gian an toàn.

- **Frame `spawn-slots.frame` (2026-11-23).** Thành và hộp hero `use` từ `feature-castle/castle-core/castle-body-values` và `feature-hero/hero-list/hero-roster-values`. Số tính ra: hộp chỗ 1/3 ở x=-2u, chỗ 2 ở x=1u (tâm ∓ nửa rộng 0.5u). Chỗ 3 cùng điểm với chỗ 1, vẽ lệch hàng (y=4.2u) chỉ để thấy cả hai.

## Chưa rõ
- Không có điểm chặn.
