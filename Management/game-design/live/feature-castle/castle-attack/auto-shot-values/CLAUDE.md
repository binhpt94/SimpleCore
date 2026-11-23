# Thông số đòn tự bắn

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Mỗi khung hình: nếu thành còn đứng, không tạm dừng, đã đủ thời gian chờ và có quái trong tầm thì bắn.
- Chọn mục tiêu: quái còn sống, trên màn (tâm |x| ≤ 16u), khoảng cách tới mép thành gần nhất ≤ 6u; nhỏ nhất thì chọn.
- Trừ máu mục tiêu ngay lúc bắn bằng sức đánh hiện tại của thành (lv1 là 10, cộng nâng cấp ở `feature-castle/castle-upgrade-effects`).
- Hiện hình viên đạn bay từ mép thành phía mục tiêu tới mục tiêu trong 0.15s; hình biến mất khi tới nơi hoặc khi mục tiêu đã biến mất.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Sát thương lv1 | 10 | máu/phát | Bộ số chung; Lính đợt 1 cần 4 phát |
| Giây mỗi phát | 1.0 | s | Bộ số chung; đủ chậm để không thay hero |
| Tầm | 6 | u, tính từ mép thành | Phủ x 1.5→7.5u mỗi bên; ngắn hơn khoảng đất 14.5u mỗi bên |
| Số mục tiêu mỗi phát | 1 | quái | Luật vòng 3 |
| Thời gian hình đạn bay | 0.15 | s | Dự kiến; chỉ để nhìn |
| Phạm vi chọn | tâm quái |x| ≤ 16 | u | Không bắn quái chưa vào màn |

Cân bằng dự kiến: 10 sát thương/s ở lv1. Đợt 10, Lính 76 máu cần 8 phát; thành một mình không giữ được nếu hero dồn hết về một bên, đúng ý "tuyến cuối".

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Sát thương lv1 | 10 | 10 (chỉnh sau chơi thử) | theo số đã chỉnh |
| Giây mỗi phát | 1.0 | 1.0 | theo số đã chỉnh |
| Tầm | 6u | 6u | theo số đã chỉnh |
| Hình đạn | 1 sprite di chuyển thẳng 0.15s | như IDEA, thêm nháy ở mục tiêu khi trúng | hiệu ứng bắn và trúng riêng, âm thanh |

## Trường hợp biên
- Hai quái cùng khoảng cách (hai bên, hoặc chồng nhau): chọn quái ra trước (thứ tự sinh nhỏ hơn).
- Mục tiêu bị hero hạ trong 0.15s hình đạn đang bay: sát thương đã tính lúc bắn; hình đạn biến mất ngay, không chuyển sang quái khác.
- Phát bắn làm quái về ≤0 máu: quái bị hạ và cho tiền như mọi quái (`feature-enemy/enemy-reward`).
- Quái bị Băng giá làm chậm: không đổi gì với đòn tự bắn.
- Tạm dừng: bộ đếm dừng, hình đạn đứng yên; bỏ tạm dừng thì chạy tiếp từ chỗ dừng.
- Thành bị phá: ngừng bắn ngay, kể cả đã đủ thời gian chờ trong cùng khung hình.
- Giữa hai đợt còn quái sót trong tầm: vẫn bắn.

## Đã tự quyết
- **Chọn lại mục tiêu mỗi phát.** Luôn bắn con nguy nhất; khác hero (hero giữ mục tiêu khi đang đánh) vì thành không phải đi. Đã loại: giữ mục tiêu.
- **Sát thương tức thì.** Một luật cho mọi đòn đánh xa trong game (`feature-hero/hero-stats/hero-level-values`).
- **Không bắn quái ngoài màn.** Người chơi không thấy thì không nên bị bắn.

- **Frame `shot-range.frame` (2026-11-23).** Giữ hằng `shot-range` = 6u (Sấm sét `use` lại). Số tính ra: tầm trái bắt đầu ở x=-7.5u = mép trái -1.5u − 6u.

## Chưa rõ
- Không có điểm chặn.
