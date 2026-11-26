# Chỉ số gốc và hệ số đợt của quái

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ bảng chỉ số gốc của 5 loại quái (asset cấu hình, không viết cứng).
- Lúc sinh quái: nhận loại và hệ số chỉ số từ `feature-wave` (không nhận số đợt); máu tối đa và sát thương = gốc × hệ số; quái ra đầy máu.
- Nhận sát thương từ hero, đòn tự bắn và Sấm sét; máu ≤ 0 thì bị hạ, báo `feature-enemy/enemy-reward` đúng một lần.
- Định nghĩa khoảng cách, nhịp đánh, sát thương theo `feature-hero/hero-stats/hero-level-values`.

## Thông số
Chỉ số gốc ở đợt 1 (dự kiến, theo bộ số chung):
| Loại | Máu | Sát thương | s mỗi đòn | Tầm (u) | Đi (u/s) | Sát thương/s |
|---|---|---|---|---|---|---|
| Lính | 40 | 5 | 1.0 | 1.0 | 1.5 | 5.0 |
| Sói | 20 | 3 | 0.8 | 1.0 | 3.0 | 3.75 |
| Khổng lồ | 150 | 15 | 1.5 | 1.2 | 0.8 | 10.0 |
| Cung thủ quái | 25 | 6 | 1.2 | 5 | 1.5 | 5.0 |
| Trùm | 400 | 40 | 2.0 | 1.5 | 0.6 | 20.0 |

Hệ số đợt (dự kiến) do `feature-wave/difficulty-scaling/wave-formula` tính: `hệ số = 1 + 0.10 × (đợt - 1)`, áp cho máu tối đa và sát thương. Bảng dưới chỉ để xem kết quả nhân; tham số nằm ở cấu hình của `feature-wave`.
| Đợt | Hệ số | Lính máu / sát thương | Khổng lồ | Trùm |
|---|---|---|---|---|
| 1 | 1.0 | 40 / 5 | 150 / 15 | (chưa có) |
| 5 | 1.4 | 56 / 7 | 210 / 21 | (chưa có) |
| 10 | 1.9 | 76 / 9.5 | 285 / 28.5 | 760 / 76 |
| 20 | 2.9 | 116 / 14.5 | 435 / 43.5 | 1160 / 116 |

| Thông số khác | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Thu nhỏ khi bị hạ | 0.3 | s | Cùng nhịp với hero gục |
| Tự hồi máu | 0 | máu/s | Luật vòng 3 |

Cân bằng dự kiến: Trùm đợt 10 có 760 máu; đội Hiệp sĩ, Cung thủ, Pháp sư cấp 5 (21.5 + 31.4 + 20.2 ≈ 73 sát thương/s) cộng thành 10 sát thương/s hạ trong khoảng 9s nếu cả đội cùng đánh.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Loại có chỉ số | Lính, Sói, Khổng lồ | cả 5 | cả 5 |
| Hệ số đợt | 1 + 0.10×(đợt-1) | như IDEA | theo số đã chỉnh sau chơi thử |
| Bị hạ | thu nhỏ 0.3s | như IDEA | anim bị hạ riêng từng loại, âm thanh |

## Trường hợp biên
- Nhiều đòn trúng cùng khung hình đưa máu xuống ≤ 0: quái bị hạ một lần, tiền một lần; các đòn sau trong khung hình bỏ qua quái này.
- Quái đã sinh ở x=±17u (ngoài màn) bị Băng giá: bị chậm, không bị hạ bởi hero (hero chưa nhắm ngoài màn).
- Quái bị hạ khi đang chậm: trạng thái chậm biến mất cùng quái.
- Sát thương có số lẻ (9.5): giữ số thực; hiện ra theo cách làm tròn ở `feature-hero/hero-stats/hero-level-values`.
- Thành bị phá: quái còn lại không bị hạ thêm, không cho tiền; `feature-run` gỡ quái khi dừng trận.

## Đã tự quyết
- **Wave giữ và tính hệ số, Enemy chỉ nhân (2026-11-24, sửa lệch theo @Manager).** Bản trước ghi Enemy nhận số đợt rồi tự tính, trái với `feature-wave/difficulty-scaling/wave-formula` và node cha (`feature-enemy/enemy-stats`: mức tăng theo đợt do `feature-wave` quyết). Một nơi giữ công thức, quái không cần biết đang đợt mấy. Đã loại: Enemy giữ công thức hệ số.
- **Hệ số đợt áp lúc sinh, không áp lại.** Quái sống qua đợt sau vẫn giữ chỉ số đợt của nó. Đã loại: tính lại theo đợt hiện tại.
- **Chỉ nhân máu và sát thương.** Giữ hình dạng trận (tốc độ, tầm) như nhau mọi đợt; khó lên nhờ số đông và độ trâu.
- **Máu Trùm gốc 800 xuống 400, sát thương 40 giữ nguyên (2026-11-23).** Theo đề xuất ở `feature-balance/balance-difficulty-curve/break-forecast`: 800 làm đợt 10 vọt quá mạnh. Đợt Trùm vẫn khó vọt hơn đợt trước, là ngoại lệ có chủ đích. Đã loại: Trùm chiếm 5 chỗ trong số quái.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ trường hợp biên tạm dừng.

## Chưa rõ
- Sói 20 máu, 3 sát thương rất yếu: một phát tự bắn cấp 1 (10) cần 2 phát, một Sấm sét hạ cả đàn tới đợt 30. Giữ theo bộ số chung; đề xuất xem lại sau chơi thử (ví dụ 30 máu).
