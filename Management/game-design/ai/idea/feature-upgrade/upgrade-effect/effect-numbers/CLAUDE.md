# Mức tăng mỗi cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Mua thành công một cấp: `feature-upgrade` báo "mục X lên cấp L" cho bên giữ chỉ số (`feature-hero` cho mục hero, `feature-castle` cho mục thành) trong cùng khung hình.
- Bên giữ chỉ số tính lại theo công thức dưới đây, luôn từ chỉ số cấp 1 (không nhân dồn từ cấp trước).
- Phần tăng của máu cộng vào cả máu tối đa và máu hiện tại (thành luôn; hero khi đang sống).
- Không có cấp nào làm giảm chỉ số.

## Thông số
| Mục | Công thức theo cấp L | Ví dụ cấp 1 / 2 / 5 / 10 | Lí do |
|---|---|---|---|
| Hero: máu tối đa | máu cấp 1 × (1 + 0,15 × (L − 1)) | 100% / 115% / 160% / 235% | Tăng đều, dễ đoán (dự kiến) |
| Hero: sát thương | sát thương cấp 1 × (1 + 0,15 × (L − 1)) | 100% / 115% / 160% / 235% | Cùng nhịp với máu (dự kiến) |
| Hero: tốc độ đánh | cấp 1 × (1 + min(0,03 × (L − 1); 0,6)) | 100% / 103% / 112% / 127% | Trần +60% ở cấp 21 để đòn không quá dày (dự kiến) |
| Thành: máu tối đa | 1000 + 150 × (L − 1) máu | 1000 / 1150 / 1600 / 2350 | Bộ số chung (dự kiến) |
| Thành: sát thương đòn tự bắn | 10 × (1 + 0,2 × (L − 1)) mỗi phát | 10 / 12 / 18 / 28 | Nhịp 1,0 giây/phát không đổi (dự kiến) |
| Sấm sét: sát thương | 80 × (1 + 0,2 × (L − 1)) | 80 / 96 / 144 / 224 | Hiệu lực = sát thương (dự kiến) |
| Băng giá: thời gian chậm | 5 × (1 + 0,2 × (L − 1)) giây, mức chậm giữ 50% | 5 / 6 / 9 / 14 giây | Hiệu lực = thời gian chậm, theo `feature-castle/castle-upgrade-effects` (dự kiến) |
| Cổ vũ: lượng hồi | 30% × (1 + 0,2 × (L − 1)) máu tối đa mỗi hero | 30% / 36% / 54% / 84% | Hiệu lực = lượng hồi (dự kiến) |
| Hồi chiêu mọi kĩ năng | không đổi theo cấp | | Quyết định vòng 3 |

- Sát thương và máu lẻ (ví dụ hero cấp 1 đánh 15 thì cấp 2 là 17,25): giữ số thực khi tính và khi trừ máu; chỉ làm tròn lúc hiện (máu làm tròn lên, sát thương làm tròn xuống), theo ý `decimal-stats` của `feature-hero/hero-stats/hero-level-values`.
- Chỉ số cấp 1 của hero do `feature-hero` giữ; con số bảng trên là tỉ lệ.
- Mức tăng mỗi cấp nằm trong asset cấu hình (`feature-balance`), không viết cứng.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Mức tăng máu, sát thương hero mỗi cấp | +15% cấp 1 | +15%, chỉnh theo chơi thử | Chốt theo `feature-balance` |
| Mức tăng tốc độ đánh hero | +3%/cấp, trần +60% | như IDEA | Chốt theo `feature-balance` |
| Máu thành mỗi cấp | +150 | như IDEA | Chốt |
| Sát thương tự bắn mỗi cấp | +20% cấp 1 | như IDEA | Chốt |
| Hiệu lực kĩ năng mỗi cấp | +20% cấp 1, cả ba kĩ năng | như IDEA | Chốt, có thể có trần cho Băng giá và Cổ vũ |
| Phản hồi thấy được khi lên cấp | hero/thành phóng to rồi về cỡ cũ trong 0,3 giây | thêm hiệu ứng lên cấp | đủ art |

## Trường hợp biên
- **Hero đang gục**: chỉ đổi cấp; hồi sinh với máu đầy theo cấp mới; thời gian chờ hồi sinh không đổi.
- **Hero đang sống, máu gần 0**: lên cấp cộng phần máu tối đa tăng vào máu hiện tại (cứu được hero sắp gục).
- **Mua máu thành khi thành còn 1 máu**: thành còn 151 máu. Thành đã về 0 máu là đã bị phá, lượt kết thúc, không mua được nữa (`feature-upgrade/upgrade-purchase/purchase-rules`).
- **Băng giá đang có hiệu lực trên quái khi lên cấp**: quái đang bị chậm giữ thời gian cũ; lần dùng sau mới theo cấp mới.
- **Sấm sét, Cổ vũ đang hồi chiêu khi lên cấp**: hồi chiêu chạy tiếp như cũ; lần dùng sau mạnh hơn.
- **Cổ vũ hồi quá máu tối đa**: máu dừng ở máu tối đa. Cổ vũ không hồi hero đang gục.
- **Tốc độ đánh hero đã chạm trần +60%**: lên cấp vẫn tăng máu và sát thương.
- **Mua sức đánh giữa lúc thành đang bắn**: phát bắn ra sau khung hình mua dùng sát thương mới; phát đang bay giữ sát thương cũ.

## Đã tự quyết
- **Cộng theo cấp 1, không nhân dồn.** Dễ đọc, dễ kiểm, không bùng nổ. Đã loại: mỗi cấp nhân 1,15 (cấp 20 gấp 14 lần).
- **Hero sống được cộng máu hiện tại.** Đồng bộ với máu thành; mua lúc nguy có tác dụng ngay. Đã loại: chỉ tăng máu tối đa cho hero.
- **Hiệu lực Băng giá là thời gian chậm, mức chậm giữ 50%.** Theo `feature-castle/castle-upgrade-effects` ("làm chậm lâu hơn"); nếu tăng mức chậm thì cấp 4 đã gần đứng yên. Đã loại: tăng phần trăm chậm.
- **Giữ số thực, chỉ làm tròn khi hiện (2026-11-23, sửa lệch).** Thống nhất với ý `decimal-stats` của `feature-hero/hero-stats/hero-level-values`, `feature-enemy/enemy-stats/enemy-stat-values` và `feature-wave/difficulty-scaling/wave-formula`: một luật làm tròn cho cả game. Đã loại: làm tròn xuống khi trừ máu, tối thiểu 1 (bản cũ, lệch với hero và quái).
- **Đối chiếu nghĩa hiệu lực kĩ năng (2026-11-23).** Khớp nghĩa đã chốt: Sấm sét +16 sát thương, Băng giá +1 giây chậm (giữ 50%), Cổ vũ +6% máu tối đa mỗi cấp, hồi chiêu không đổi. Chỉ sửa ghi chú Băng giá ở "Chưa rõ" (hồi chiêu 35 giây đã biết).

## Chưa rõ
- Đề xuất cho người: Băng giá không có trần, cấp 11 làm chậm 15 giây. Hồi chiêu Băng giá là 35 giây (`feature-castle/castle-skills/skill-values`), nên chỉ từ cấp 31 quái mới bị chậm liên tục; giá cấp đó không đạt được trong một lượt bình thường. Hiện vẫn dùng công thức không trần.
- Đề xuất cho người: Cổ vũ cấp 13 hồi 102%, tức hồi đầy từ cấp đó; có thể chấp nhận vì giá kĩ năng tăng nhanh (lên cấp 13 cần tổng khoảng 15.400 tiền).
