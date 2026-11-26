# Công thức độ khó theo đợt

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Đầu mỗi đợt, từ số đợt w tính ra: số quái, nhịp thả, hệ số chỉ số, số con từng loại (kể cả Trùm). Kết quả giao cho `feature-wave/spawning/spawn-order` để lập danh sách thả.
- Mọi tham số (số quái gốc, số quái thêm mỗi đợt, nhịp gốc, mức giảm, sàn, hệ số tăng, mốc mở loại, chu kì Trùm, bảng phần trăm) nằm trong cấu hình của `feature-wave`; giá trị do `feature-balance` quyết.
- Không giữ chỉ số gốc của quái. Node này giữ công thức và tính hệ số chỉ số; hệ số đi kèm từng quái lúc sinh, `feature-enemy` nhận hệ số (không nhận số đợt) và nhân với chỉ số gốc (`feature-enemy/enemy-stats/enemy-stat-values`).

## Thông số
| Thông số | Giá trị (dự kiến) | Đơn vị | Lí do |
|---|---|---|---|
| Số quái | 6 + 2×w | con | Đợt 1 đủ ít để làm quen; tăng đều, luôn chẵn để chia đôi hai bên |
| Nhịp thả | max(0.3, 1.0 − 0.05×(w−1)) | giây | Dày dần; sàn 0.3 s để màn hình vẫn đọc được từng con |
| Hệ số máu, sức đánh | 1 + 0.10×(w−1) | lần | Tăng tuyến tính, nhân với số quái tăng thành áp lực tăng nhanh hơn sức đội mua được |
| Mốc mở loại | Lính 1, Sói 3, Khổng lồ 5, Cung thủ quái 7 | đợt | Mỗi 2 đợt gặp một loại mới, kịp quen loại cũ |
| Chu kì Trùm | w chia hết cho 10, 1 con | đợt | Đỉnh căng thẳng hiếm, dễ nhớ |
| Bảng phần trăm | xem data.md, ý `type-mix` | % số chỗ | Phần Khổng lồ, Cung thủ quái tăng theo bậc; Lính giảm dần |
| Làm tròn | phần nguyên, tối thiểu 1 cho loại đã mở; dư cho Lính | con | Tổng luôn đúng số quái; không loại đã mở nào vắng mặt |

Ví dụ kết quả (dự kiến, tính theo công thức trên):

| Đợt | Số quái | Nhịp (s) | Hệ số | Lính | Sói | Khổng lồ | Cung thủ quái | Trùm | Tiền hạ quái cả đợt (xu) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 8 | 1.0 | 1.0 | 8 | 0 | 0 | 0 | 0 | 40 |
| 3 | 12 | 0.9 | 1.2 | 9 | 3 | 0 | 0 | 0 | 57 |
| 5 | 16 | 0.8 | 1.4 | 10 | 4 | 2 | 0 | 0 | 96 |
| 7 | 20 | 0.7 | 1.6 | 9 | 5 | 3 | 3 | 0 | 128 |
| 10 | 26 | 0.55 | 1.9 | 11 | 6 | 5 | 3 | 1 | 272 |
| 15 | 36 | 0.3 | 2.4 | 13 | 9 | 7 | 7 | 0 | 248 |
| 20 | 46 | 0.3 | 2.9 | 16 | 11 | 9 | 9 | 1 | 413 |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Số quái | 6 + 2×w | 6 + 2×w, chỉnh sau chơi thử | chốt sau chơi thử |
| Nhịp thả | max(0.3, 1.0 − 0.05×(w−1)) s | như IDEA, chỉnh sau chơi thử | chốt sau chơi thử |
| Hệ số máu, sức đánh | 1 + 0.10×(w−1) | như IDEA, chỉnh sau chơi thử | chốt sau chơi thử |
| Loại quái dùng | Lính, Sói, Khổng lồ theo mốc; không Trùm, không Cung thủ quái (chỗ của chúng dồn cho Lính) | đủ 5 loại | đủ 5 loại |
| Bảng phần trăm | đủ 6 bậc | đủ 6 bậc, chỉnh sau chơi thử | chốt sau chơi thử |
| Tham số đặt trong cấu hình | có | có | có |

## Trường hợp biên
- **Đợt rất cao (w ≥ 100):** công thức vẫn chạy, không có trần; số quái 206 con ở đợt 100 vẫn thả được vì giới hạn quái sống cùng lúc nằm ở `feature-wave/spawning/spawn-order`.
- **Phần trăm làm tròn ra 0 cho loại đã mở:** nâng lên 1, bớt 1 Lính.
- **Phần nguyên các loại khó cộng lại vượt số chỗ:** không xảy ra với bảng hiện tại (tổng loại khó tối đa 65%); nếu cấu hình đổi làm xảy ra thì bớt từ loại có phần trăm thấp nhất cho tới khi Lính còn ít nhất 1. Cấu hình sai (tổng phần trăm khác 100%) thì báo lỗi lúc nạp cấu hình, nêu tên bậc sai.
- **Đợt Trùm:** Trùm chiếm 1 chỗ; phần trăm các loại khác tính trên số chỗ còn lại (đợt 10: 25 chỗ).
- **Chỉ số sau nhân ra số lẻ:** máu và sức đánh giữ số thực, chỉ làm tròn khi hiện ra, theo luật chung ở `feature-hero/hero-stats/hero-level-values` (Lính đợt 2: 44 máu, 5.5 sức đánh).

## Đã tự quyết
- **Bảng phần trăm theo bậc, không công thức liên tục.** Dễ đọc, dễ chỉnh, kiểm bằng ví dụ được. Đã loại: phần trăm tăng từng đợt theo công thức (khó đoán số con).
- **Tiền mỗi con không nhân theo đợt (theo bộ số chuẩn, khớp ý `scale-reward` của node cha).** Tiền trung bình mỗi con tăng nhờ phần quái trâu tăng: 40/8 = 5 xu ở đợt 1, 413/46 ≈ 9 xu ở đợt 20. Đã loại: thêm hệ số tiền theo đợt.
- **Có Trùm ở đợt chia hết cho 10 (theo bộ số chuẩn và `feature-enemy/enemy-list`).** Mục "Đã tự quyết" của node cha ghi "không có đợt trùm"; bộ số chuẩn và `feature-enemy/enemy-list` (Trùm, "khi nào trùm ra là việc của `feature-wave`") quyết khác, node này theo bộ số chuẩn.
- **Chỉ số giữ số thực, làm tròn khi hiện (2026-11-23, sửa lệch).** Bản cũ làm tròn lên số nguyên, trái với ý `decimal-stats` của `feature-hero/hero-stats/hero-level-values` và với `feature-enemy/enemy-stats/enemy-stat-values` (Lính đợt 10: 9.5 sát thương). Một luật cho hero, quái và thành. Đã loại: làm tròn lên lúc nhân.
- **Mức IDEA chỉ có Lính, Sói, Khổng lồ, không có Trùm (2026-11-23, sửa lệch).** Theo bảng giai đoạn của `feature-enemy/enemy-list/enemy-type-values`. Công thức giữ nguyên; ở IDEA, chỗ của Cung thủ quái và Trùm dồn cho Lính.
- **Mốc mở loại và chu kì Trùm chỉ nằm ở node này (2026-11-25).** Node loại quái của `feature-enemy` đã bỏ bản chép "đợt đầu có mặt" và "Trùm ở đợt 10, 20..." (theo hướng A người chọn); node đó chỉ còn loại nào có hình ở từng giai đoạn, khớp hàng "Loại quái dùng" ở bảng giai đoạn trên. Không đổi số nào.
- **Thu giảm sau đợt Trùm là ngoại lệ có chủ đích (2026-11-23).** Sửa chi tiết ý `reward-by-mix` (trước ghi "thu vẫn tăng không giảm", sai với bảng ví dụ: đợt 11 thu 187 < đợt 10 thu 272). Máu Trùm gốc đổi 800 → 400 (`feature-enemy/enemy-stats/enemy-stat-values`) không đổi số nào trong node này vì node chỉ ghi hệ số, không ghi máu.

## Chưa rõ
- Không có điểm chặn. (Ý `scale-reward` của node cha đã sửa cho khớp: tiền mỗi con giữ nguyên, 2026-11-23.)
