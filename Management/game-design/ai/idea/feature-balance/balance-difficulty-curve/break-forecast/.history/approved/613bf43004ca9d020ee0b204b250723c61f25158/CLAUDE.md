# Dự đoán đợt gãy

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Tính áp lực P cho các đợt mẫu mỗi khi bộ số đổi; so với mục tiêu: đợt 1–9 P < 1.1, không có hai đợt liền nhau mà P chênh quá 0.5 (trừ đợt Trùm, ngoại lệ có chủ đích), người chơi bình thường gãy ở đợt 15–19.
- Sau chơi thử: ghi đợt gãy thật của vài lượt, so với dự đoán, rồi chỉnh số ở `feature-balance/balance-config/number-table`.

## Thông số

### Công thức (dự kiến)
- Máu quái đợt w: H(w) = Σ (máu gốc × số con) × (1 + 0.10×(w−1)).
- Thời gian thả: T(w) = (N − 1) × nhịp thả, với N = 6 + 2×w (`feature-wave/spawning/spawn-order`).
- Sát thương mỗi giây hiệu dụng của đội: D = Σ hero (sát thương/giây lv1 × m(L), đòn vùng ×2) + 10×(1 + 0.2×(cấp sức đánh − 1)) + 80×(1 + 0.2×(cấp Sấm − 1)) × số quái trúng ÷ 20.
- m(L) = (1 + 0.15×(L−1)) × (1 + min(0.03×(L−1), 0.6)): cấp 3 = 1.38, cấp 5 = 1.79, cấp 7 = 2.24, cấp 9 = 2.73.
- Áp lực: P(w) = H(w) ÷ (D × T(w)).
- Sát thương mỗi giây của quái nếu tất cả cùng đánh: E(w) = Σ (sát thương gốc ÷ giây mỗi đòn × số con) × (1 + 0.10×(w−1)). Dùng để thấy quái dồn tới thành nguy cỡ nào.

### Đội mẫu và cách mua
Đội Hiệp sĩ + Cung thủ + Pháp sư (một chắn, một bắn xa, một đòn vùng). Tổng sát thương/giây hiệu dụng lv1 của 3 hero: 12 + 17.5 + 11.25×2 = 52. Người chơi tiêu hết tiền ở đầu mỗi đợt theo `feature-balance/balance-economy/income-and-prices`, khoảng 55% cho hero, 45% cho thành và kĩ năng.

### Kết quả tính thử (dự kiến)
| Đợt | Tiền đầu đợt (xu) | Đội đã mua | D (sát thương/giây) | H tổng máu quái | T thả (s) | P | E quái (sát thương/giây) | Máu thành tối đa | Tổng máu 3 hero |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 50 | chưa nâng | 78 | 320 | 7 | 0.6 | 40 | 1000 | 520 |
| 5 | 393 | hero cấp 3; máu, sức đánh, Sấm cấp 2 | 103 | 1092 | 12 | 0.9 | 119 | 1150 | 676 |
| 9 | 1070 | hero cấp 5; máu thành 4, sức đánh 3, Sấm 3 | 135 | 2025 | 13.8 | 1.1 | 230 | 1450 | 832 |
| 10 (Trùm) | 1282 | hero cấp 5/5/6; máu, sức đánh 4; Sấm 3; Cổ vũ 2 | 140 | 3391 | 13.75 | 1.8 | 309 | 1450 | 877 |
| 11 | 1624 | hero cấp 6; máu 5, sức đánh 4; Sấm 3; Cổ vũ 2 | 149 | 2940 | 13.5 | 1.5 | 313 | 1600 | 910 |
| 15 | 2791 | hero cấp 7 (một con 8); máu, sức đánh 6; Sấm 4; Cổ vũ, Băng 2 | 173 | 4620 | 10.5 | 2.5 | 489 | 1750 | 988 |
| 19 | 4273 | hero cấp 9; máu, sức đánh 6; Sấm 5; Cổ vũ, Băng 2 | 205 | 6440 | 12.9 | 2.4 | 690 | 1750 | 1144 |
| 20 (Trùm) | 4685 | hero cấp 9; máu, sức đánh 7; Sấm 5; Băng 3; Cổ vũ 2 | 207 | 8222 | 13.5 | 2.9 | 801 | 1900 | 1144 |

Đọc bảng:
- Đợt 1–9: P ≤ 1.1, đội hạ gần kịp nhịp thả; thành mất ít hoặc không mất máu.
- Đợt 10: Trùm (760 máu, 76 sát thương mỗi đòn) chiếm khoảng 22% máu cả đợt; P vọt lên 1.8 rồi rơi về 1.5 ở đợt 11. Đây là ngoại lệ có chủ đích (đỉnh căng thẳng); người chơi vụng thường gãy ở đây.
- Đợt 11–15: P lên đều tới 2.5; máu thành không tự hồi nên mất mát cộng dồn. Đợt 15 nhịp thả chạm sàn 0.3 s, quái dồn nhanh hơn.
- Đợt 15–19: P quanh 2.4–2.6, sức quái nhân hệ số tăng tuyến tính trong khi sức đội chỉ tăng theo bậc thang giá mũ; người chơi bình thường gãy trong khoảng này.
- Đợt 20: Trùm 1160 máu (khoảng 14% máu cả đợt); P ≈ 2.9, E quái 801/giây so với 1900 máu thành; nếu Trùm và đám đông tới thành cùng lúc, thành vỡ trong vài giây.

### Độ dài lượt (dự kiến)
Mỗi đợt mất T × (1 + P) (thời gian thả cộng thời gian dọn quái tồn, khoảng T × P) cộng 3 s lúc thở: đợt 1 khoảng 14 s, đợt 5 khoảng 26 s, đợt 10 khoảng 41 s, đợt 15–19 khoảng 40–47 s (P các đợt giữa hai mẫu lấy nội suy tuyến tính). Cộng dồn: hết đợt 9 khoảng 3.7 phút, hết đợt 14 khoảng 7.0 phút, hết đợt 18 khoảng 9.8 phút, hết đợt 19 khoảng 10.6 phút. Gãy ở đợt 15–19 (thua giữa đợt đó): lượt dài khoảng 7–10.5 phút. Khớp ước tính của `feature-wave/wave-gap/gap-timer` (gãy ở đợt 15: khoảng 7–8 phút).

### Ba cách chơi để chơi thử (tester mô phỏng đúng như sau)
Chung cho cả ba: đội Hiệp sĩ + Cung thủ + Pháp sư; mua và bấm chỉ khi trận đang chạy.

| | Vụng | Thường | Khéo |
|---|---|---|---|
| Lúc mua | Mỗi khi đủ tiền | Đầu mỗi lúc thở, tiêu hết | Mỗi khi đủ tiền cho món kế tiếp trong thứ tự ưu tiên; không để tiền nằm yên quá 3 giây khi đủ giá món đó |
| Mua gì | Món rẻ nhất lúc đó | Rải đều: 55% hero, 45% thành và kĩ năng | Theo thứ tự ưu tiên ở dưới |
| Sấm sét | Bấm ngay khi hồi xong | Bấm khi vùng quanh thành có từ 4 quái | Bấm khi vùng quanh thành có từ 6 quái (đợt 1–5: từ 4); hoặc khi máu thành dưới 30% và có quái đang đánh thành |
| Băng giá | Bấm ngay khi hồi xong | Bấm khi có từ 15 quái sống | Đợt Trùm: giữ tới khi Trùm cách mép thành 4u rồi bấm. Đợt khác: bấm khi có từ 20 quái sống |
| Cổ vũ | Bấm ngay khi hồi xong | Bấm khi một hero dưới 40% máu | Bấm khi từ 2 hero dưới 40% máu, hoặc Hiệp sĩ dưới 30% |

Thứ tự ưu tiên mua của cách chơi khéo (mỗi lần mua lấy dòng đầu tiên còn đúng):
1. Máu thành, nếu máu thành hiện tại dưới 50% máu tối đa.
2. Pháp sư (đòn vùng), nếu cấp của nó không cao hơn cấp hero thấp nhất quá 1.
3. Sức đánh thành, nếu cấp của nó thấp hơn cấp Pháp sư trừ 2.
4. Sấm sét, nếu cấp của nó thấp hơn cấp Pháp sư chia 3, làm tròn lên.
5. Hero có cấp thấp nhất (hoà thì Hiệp sĩ trước).
6. Máu thành.
Trước đợt Trùm (đợt 9, 19, …) để dành đủ giá Băng giá cấp kế nếu Băng giá còn cấp 1.

Đo khoảng cách: mỗi cách chơi ít nhất 5 lượt cùng seed dev, ghi đợt gãy; so trung vị.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Mục tiêu đợt gãy người chơi bình thường | không kiểm | đợt 15–19 | chốt sau chơi thử |
| Mục tiêu P đợt 1–9 | không kiểm | dưới 1.1 | chốt sau chơi thử |
| Kiểm bằng chơi thử | không bắt buộc | ít nhất 5 lượt mỗi cách chơi (vụng, thường, khéo), ghi đợt gãy | ít nhất 20 lượt, nhiều đội khác nhau |
| Khoảng cách khéo so với vụng (trung vị đợt gãy) | không kiểm | ít nhất 4 đợt | ít nhất 6 đợt |

## Trường hợp biên
- **Công thức P là ước lượng thô:** không tính quãng đi của quái (khoảng 10 s với Lính), không tính hero chết và hồi sinh 8 s, không tính Băng giá và Cổ vũ. Dùng để so các đợt với nhau và thấy xu hướng, không để đoán chính xác một lượt.
- **Đợt có giới hạn 40 quái sống** (từ đợt 17 trở đi N ≥ 40): khi đội yếu, việc hoãn thả kéo dài T và làm P thật thấp hơn bảng; đợt gãy thật có thể muộn hơn khoảng 1 đợt.
- **Đội lệch toàn cận chiến** (Hiệp sĩ + Đấu sĩ rìu + Kiếm khách): D lv1 = 12 + 20 + 22.9 = 54.9, gần đội mẫu; khó hơn từ đợt 7 vì phải đuổi Cung thủ quái. Xem `feature-balance/balance-team/hero-compare`.

## Đã tự quyết
- **Thước đo áp lực là máu quái so với sát thương đội làm ra trong lúc thả.** Đơn giản, tính tay được, chỉ ra được đợt nào vọt. Đã loại: mô phỏng đầy đủ (ngoài phạm vi, `feature-balance/balance-config` không có công cụ mô phỏng).
- **Đội mẫu trộn ba cách đánh, mua rải đều.** Đại diện người chơi bình thường.
- **Không đổi bộ số chuẩn trong node này.** Đề xuất đổi ở "Chưa rõ".
- **Tính lại đợt 10 và 20 theo máu Trùm gốc 400 (2026-11-23).** Đợt 10: H 4151 → 3391, P 2.2 → 1.8. Đợt 20: H 9382 → 8222, P 3.4 → 2.9. Sửa ý `normal-break` cho khớp P đợt 20. Bước P đợt 9 → 10 (1.1 → 1.8) vẫn hơn 0.5: chấp nhận vì đợt Trùm là ngoại lệ có chủ đích của `curve-smooth`.

- **Sửa hai số lệch công thức (2026-11-25, phiên Balance báo qua @Manager).** E đợt 19: 650 → 690; tính lại theo `feature-wave/difficulty-scaling/wave-formula` (44 con: Lính 17, Sói 11, Khổng lồ 8, Cung thủ quái 8) và `feature-enemy/enemy-stats/enemy-stat-values`: (5×17 + 3.75×11 + 10×8 + 5×8) × 2.8 = 689.5. Các hàng E khác, mọi hàng H, T, P đã rà lại và khớp. Độ dài lượt: bản cũ ghi "đợt 10 khoảng 30 s, 7–9 phút", không khớp chính công thức T × (1 + P) + 3; tính lại ra 7–10.5 phút, sửa ý `normal-break`. Không đổi bộ số chuẩn, không đổi đợt gãy dự kiến.

- **Sửa `skill-gap` theo chơi thử PROTOTYPE (2026-11-26, @Manager chuyển kết quả `Tester/UnityAutoPlay/20261126-010423`: 6 lượt tua x16, vụng đợt 10–12, thường 14, khéo 12–13).** Lỗi nằm ở hai chỗ:
  - **Dự báo:** "khéo tới 20–25" không có cơ sở trong chính bảng tính (đợt 20 P ≈ 2.9 cả với cách chơi thường; Băng giá, Cổ vũ chưa vào công thức). Sửa thành mục tiêu đo được: khéo hơn vụng ít nhất 4 đợt, khéo khoảng 16–19.
  - **Định nghĩa khéo:** bot "khéo" mua món rẻ nhất nên chơi không khác vụng, còn kém cách chơi thường (mua rải đều). Ghi bảng ba cách chơi có thể mô phỏng được (lúc mua, mua gì, ngưỡng bấm từng kĩ năng, thứ tự ưu tiên mua).
  - **Đợt gãy của cách chơi thường đo được 14, sát dưới khoảng 15–19:** chấp nhận, chưa đổi số.
  - **Chưa đổi số của feature nào.** Chạy lại với ba cách chơi mới trước. Nếu khéo vẫn hơn vụng dưới 4 đợt thì chỉnh theo thứ tự, mỗi lần một bước rồi chạy lại: (1) Sấm sét sát thương gốc 80 → 110, hồi chiêu giữ 20 giây (bấm đúng lúc dồn đông được thưởng nhiều hơn bấm bừa); (2) Băng giá làm chậm 50% → 65% (giữ cho Trùm đáng hơn); (3) giá kĩ năng thành hệ số 1.5 → 1.4. Ba số đó thuộc `feature-castle` (kĩ năng) và `feature-upgrade` (giá).
  - Đã loại: hạ độ khó chung (giảm hệ số +0.10 mỗi đợt): kéo cả vụng lẫn khéo lên, không nới khoảng cách.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ "từ chối hồi sinh" khỏi luật chung của ba cách chơi thử.

## Chưa rõ
- **Đợt Trùm vọt quá mạnh: đã áp dụng (2026-11-23).** Máu Trùm gốc giảm 800 → 400, giữ sát thương 40 (`feature-enemy/enemy-stats/enemy-stat-values`); P đợt 9 → 10 → 11 còn 1.1 → 1.8 → 1.5, đợt 20 còn 2.9. Đợt Trùm là ngoại lệ có chủ đích của `curve-smooth`. Đã loại: Trùm chiếm 5 chỗ trong số quái.
- **Mục "không có đợt đặc biệt" của `feature-wave/difficulty-scaling`:** đã sửa thành ngoại lệ đợt Trùm (2026-11-23).
Đề xuất cho agent chính quyết (chưa áp dụng):
- **Nếu muốn lượt ngắn hơn** (khoảng 5 phút, gãy ở đợt 10–14): tăng hệ số máu, sát thương quái từ +0.10 lên +0.12 mỗi đợt. Hiện tại giữ +0.10 vì lượt 7–10.5 phút vẫn hợp "lượt ngắn".
- Băng giá và Cổ vũ chưa vào công thức; chơi thử sẽ cho biết chúng kéo đợt gãy được bao nhiêu.
