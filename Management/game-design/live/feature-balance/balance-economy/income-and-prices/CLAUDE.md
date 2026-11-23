# Thu theo đợt và giá nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Dùng công thức dưới đây để kiểm bộ số chuẩn mỗi khi đổi tiền quái, thưởng dọn đợt, số quái hay giá nâng cấp.
- Các số nằm ở cấu hình của `feature-enemy` (tiền quái), `feature-wave` (số quái, thưởng dọn đợt), `feature-upgrade` (giá), `feature-wallet` (tiền đầu lượt); bảng đối chiếu ở `feature-balance/balance-config/number-table`.

## Thông số

### Công thức (dự kiến)
- Tiền hạ quái của đợt w: K(w) = 5×Lính + 4×Sói + 15×Khổng lồ + 6×Cung thủ quái + 100×Trùm (số con theo `feature-wave/difficulty-scaling/wave-formula`).
- Thưởng dọn đợt: B(w) = 20 + 5×w.
- Thu của đợt: I(w) = K(w) + B(w).
- Tiền có ở đầu đợt w: M(w) = 50 + Σ I(1…w−1).
- Giá nâng từ cấp c: hero ⌈30×1.35^(c−1)⌉; máu, sức đánh thành ⌈40×1.4^(c−1)⌉; mỗi kĩ năng ⌈60×1.5^(c−1)⌉.

### Thu theo đợt (dự kiến, người chơi hạ hết quái)
| Đợt | K tiền quái | B thưởng | I thu | M đầu đợt | Tiền trung bình mỗi con |
|---|---|---|---|---|---|
| 1 | 40 | 25 | 65 | 50 | 5.0 |
| 2 | 50 | 30 | 80 | 115 | 5.0 |
| 3 | 57 | 35 | 92 | 195 | 4.8 |
| 4 | 66 | 40 | 106 | 287 | 4.7 |
| 5 | 96 | 45 | 141 | 393 | 6.0 |
| 6 | 105 | 50 | 155 | 534 | 5.8 |
| 7 | 128 | 55 | 183 | 689 | 6.4 |
| 8 | 138 | 60 | 198 | 872 | 6.3 |
| 9 | 147 | 65 | 212 | 1070 | 6.1 |
| 10 | 272 | 70 | 342 | 1282 | 10.5 |
| 11 | 187 | 75 | 262 | 1624 | 6.7 |
| 12 | 207 | 80 | 287 | 1886 | 6.9 |
| 13 | 216 | 85 | 301 | 2173 | 6.8 |
| 14 | 227 | 90 | 317 | 2474 | 6.7 |
| 15 | 248 | 95 | 343 | 2791 | 6.9 |
| 16 | 258 | 100 | 358 | 3134 | 6.8 |
| 17 | 278 | 105 | 383 | 3492 | 7.0 |
| 18 | 288 | 110 | 398 | 3875 | 6.9 |
| 19 | 297 | 115 | 412 | 4273 | 6.8 |
| 20 | 413 | 120 | 533 | 4685 | 9.0 |

### Giá và tổng tiền tới cấp (dự kiến)
| Lên cấp | Hero: giá / cộng dồn | Máu hoặc sức đánh thành: giá / cộng dồn | Một kĩ năng: giá / cộng dồn |
|---|---|---|---|
| 2 | 30 / 30 | 40 / 40 | 60 / 60 |
| 3 | 41 / 71 | 56 / 96 | 90 / 150 |
| 4 | 55 / 126 | 79 / 175 | 135 / 285 |
| 5 | 74 / 200 | 110 / 285 | 203 / 488 |
| 6 | 100 / 300 | 154 / 439 | 304 / 792 |
| 7 | 135 / 435 | 216 / 655 | 456 / 1248 |
| 8 | 182 / 617 | 302 / 957 | 684 / 1932 |
| 9 | 246 / 863 | 422 / 1379 | 1026 / 2958 |
| 10 | 331 / 1194 | 591 / 1970 | 1538 / 4496 |

### Lời mỗi xu ở lần nâng đầu (dự kiến)
| Mục | Thêm vào | Giá | Lời |
|---|---|---|---|
| Cung thủ cấp 1→2 | +3.2 sát thương/giây, +18 máu | 30 | 0.11 sát thương/giây mỗi xu |
| Hiệp sĩ cấp 1→2 | +2.2 sát thương/giây, +45 máu | 30 | 0.07 và 1.5 máu mỗi xu |
| Cung thủ cấp 4→5 | +3.7 sát thương/giây | 74 | 0.05 |
| Sức đánh thành 1→2 | +2 sát thương/giây | 40 | 0.05 |
| Sức đánh thành 4→5 | +2 sát thương/giây | 110 | 0.018 |
| Sấm sét 1→2 | +16 mỗi lần × khoảng 4 quái / 20 s ≈ +3.2 | 60 | 0.053 |
| Máu thành 1→2 | +150 máu không tự hồi | 40 | 3.75 máu mỗi xu |

Đọc bảng: hero cấp thấp lời nhất, nên người chơi mua hero trước; tới cấp 4–5 thì lời của hero ngang lần nâng đầu của thành và kĩ năng, kéo người chơi rải tiền. Máu thành là mục duy nhất bù được máu đã mất, nên luôn có lí do mua.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Tiền quái, thưởng, giá | theo bộ số chuẩn | bộ số chuẩn, chỉnh sau chơi thử | chốt sau chơi thử |
| Kiểm bảng thu so với chơi thật | không bắt buộc | ghi tiền thật ở đầu đợt 5, 10, 20 của vài lượt thử, so với cột M | chốt |

## Trường hợp biên
- **Người chơi không hạ hết quái** (thua giữa đợt): tiền chỉ tính phần quái đã hạ, không có thưởng dọn đợt; bảng trên là mức trần.
- **Đợt Trùm:** thu vọt (đợt 10: 342 xu) rồi đợt sau thấp hơn (đợt 11: 262 xu). Đây là chỗ duy nhất thu đợt sau nhỏ hơn đợt trước; là ngoại lệ có chủ đích theo luật `feature-balance/balance-economy` (Trùm là phần thưởng riêng).
- **Người chơi giữ tiền không tiêu:** không có lãi, không mất; tiền nằm yên tới khi mua hoặc hết lượt.
- **Giá ở cấp rất cao** (nâng hero từ cấp 29 lên 30: khoảng 133 000 xu): vẫn tính theo công thức, không có trần; thực tế không ai tới.

## Đã tự quyết
- **Tính theo người chơi hạ hết quái và tiêu hết tiền.** Cho mức trần của sức đội; người chơi thật luôn thấp hơn một chút.
- **Đo "lời" bằng sát thương mỗi giây thêm vào chia giá.** Một thước đo chung cho hero, thành, kĩ năng. Máu đo riêng vì không đổi ra sát thương được.
- **Thu giảm sau đợt Trùm theo phương án (a) (2026-11-23).** Giữ bảng thu; máu Trùm gốc đổi 800 → 400 không đổi số nào ở node này (tiền Trùm vẫn 100 xu).

## Chưa rõ
- **Thu giảm sau đợt Trùm** (đợt 10 → 11: 342 → 262 xu): đã quyết theo phương án (a) (2026-11-23): ngoại lệ có chủ đích, Trùm là phần thưởng riêng; luật node cha `feature-balance/balance-economy` đã thêm ngoại lệ. Tiền Trùm giữ 100 xu. Đã loại: (b) giảm tiền Trùm xuống 20 xu.
- **Sức đánh thành yếu dần nhanh:** mỗi cấp chỉ +2 sát thương/giây trong khi giá ×1.4; từ cấp 5 lời chỉ còn 0.018, gần như không ai mua. Đề xuất: giữ +20% lv1 mỗi cấp nhưng đổi hệ số giá sức đánh thành từ 1.4 xuống 1.3, hoặc tăng mức thêm lên +30% lv1 mỗi cấp.
- **Ý `scale-reward` của `feature-wave/difficulty-scaling`:** đã sửa cho khớp bộ số chuẩn (2026-11-23): tiền mỗi con không nhân theo đợt.
