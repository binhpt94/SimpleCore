# So sánh sức hero

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Tính sức tổng S của mỗi hero mỗi khi đổi chỉ số hero ở `feature-hero`; giữ mọi hero trong ±10% quanh trung bình.
- Giữ bảng hợp khó; mỗi khi thêm hay đổi loại quái (`feature-enemy`) thì rà lại để mỗi hero vẫn có ít nhất một loại hợp, một loại khó.
- Số chuẩn lấy ở `feature-balance/balance-config/number-table`.

## Thông số

### Công thức (dự kiến)
- Sát thương mỗi giây = sát thương ÷ giây mỗi đòn.
- Sát thương mỗi giây hiệu dụng = sát thương mỗi giây × 2 nếu đòn vùng, × 1 nếu một mục tiêu.
- Máu hiệu dụng = máu × 2 nếu tầm ≥ 5u, × 1 nếu cận chiến.
- S = √(máu hiệu dụng × sát thương mỗi giây hiệu dụng).

### Kết quả lv1 (dự kiến)
| Hero | Máu | Máu hiệu dụng | Sát thương/giây | Hiệu dụng | S | So trung bình 65 |
|---|---|---|---|---|---|---|
| Hiệp sĩ | 300 | 300 | 12.0 | 12.0 | 60.0 | −8% |
| Đấu sĩ rìu | 240 | 240 | 10.0 | 20.0 | 69.3 | +6% |
| Kiếm khách | 180 | 180 | 22.9 | 22.9 | 64.1 | −1% |
| Cung thủ | 120 | 240 | 17.5 | 17.5 | 64.8 | 0% |
| Pháp sư | 100 | 200 | 11.25 | 22.5 | 67.1 | +3% |

### Mạnh và yếu (so trong 5 hero)
| Hero | Mạnh | Yếu |
|---|---|---|
| Hiệp sĩ | máu cao nhất (300) | sát thương mỗi đòn thấp nhất (12), tầm ngắn nhất (1.2u, bằng Kiếm khách) |
| Đấu sĩ rìu | đòn vùng cận chiến, máu cao nhì (240) | đánh chậm nhì (1.4 s), sát thương mỗi mục tiêu thấp nhất (10) |
| Kiếm khách | sát thương mỗi giây cao nhất (22.9), đi nhanh nhất (3.5 u/s) | máu thấp nhất trong cận chiến (180) |
| Cung thủ | tầm xa nhất (7u) | máu thấp nhì (120), một mục tiêu |
| Pháp sư | nổ vùng bán kính 1.5u, tầm 6u | máu thấp nhất (100), đánh chậm nhất (1.6 s), đi chậm nhất (2.2 u/s) |

### Bảng hợp khó với loại quái (dự kiến, ở cùng cấp)
| Hero | Hợp | Lí do | Khó | Lí do |
|---|---|---|---|---|
| Hiệp sĩ | Khổng lồ, Trùm | chịu được: Khổng lồ 10 sát thương/giây cần 30 s để hạ Hiệp sĩ | Cung thủ quái | tầm 1.2u, phải đi tới quái đứng cách 5u |
| Đấu sĩ rìu | Lính, Sói theo đám | chém vùng 1.5u trúng nhiều con | Cung thủ quái, Trùm | đánh chậm, sát thương một con thấp |
| Kiếm khách | Cung thủ quái, Khổng lồ | đi 3.5 u/s đuổi kịp; hạ Khổng lồ 150 máu trong khoảng 6.3 s | đám Sói, Lính đông | một mục tiêu, 180 máu bị vây |
| Cung thủ | Cung thủ quái, Trùm | tầm 7u bắn trước tầm 5u của quái; 17.5 sát thương/giây vào một con | Sói | đi 3.0 u/s, tới sát nhanh, Cung thủ chỉ 120 máu |
| Pháp sư | đám Lính, Sói | nổ 1.5u trúng nhiều con, 18 sát thương hạ Sói 20 máu trong 2 đòn | Khổng lồ, Trùm | 11.25 sát thương/giây vào một con, cần khoảng 12.8 s cho một Khổng lồ |

### Tăng theo cấp
Mọi hero: cấp L nhân máu và sát thương với (1 + 0.15×(L−1)), tốc độ đánh với (1 + min(0.03×(L−1), 0.6)). S tăng cùng tỉ lệ cho mọi hero: S(L) = S(1) × √((1 + 0.15×(L−1)) × m(L)), với m(L) như ở `feature-balance/balance-difficulty-curve/break-forecast`. Cấp 5: √(1.6 × 1.792) = ×1.69; cấp 9: √(2.2 × 2.728) = ×2.45.

### Đội và sát thương mỗi giây hiệu dụng lv1 (dự kiến)
| Đội | Tổng | Ghi chú |
|---|---|---|
| Hiệp sĩ + Cung thủ + Pháp sư | 52.0 | đội mẫu của dự đoán đợt gãy |
| Kiếm khách + Cung thủ + Pháp sư | 62.9 | đánh mạnh, ít người chắn |
| Hiệp sĩ + Đấu sĩ rìu + Kiếm khách | 54.9 | đội lệch toàn cận chiến |
| Hiệp sĩ + Đấu sĩ rìu + Pháp sư | 54.5 | mạnh với đám đông, yếu với Trùm |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Số hero cân | 5 | 5 | 5 |
| Ngưỡng lệch S quanh trung bình | không kiểm | ±10% | ±10%, kiểm lại bằng chơi thử nhiều đội |
| Bảng hợp khó | không kiểm | rà theo bảng | rà theo bảng và đợt gãy thật của từng đội |

## Trường hợp biên
- **Đòn vùng trúng 1 con** (quái thưa ở đợt đầu): Đấu sĩ rìu và Pháp sư yếu hơn bảng; chấp nhận vì đợt đầu dễ.
- **Đòn vùng trúng hơn 2 con** (quái dồn ở đợt sau): Đấu sĩ rìu và Pháp sư mạnh hơn bảng; đây là chỗ hai hero này tỏa sáng, đúng ý `team-counter`.
- **Hero bắn xa bị Sói tới sát:** máu hiệu dụng ×2 không còn đúng; bảng hợp khó đã ghi Sói là loại khó của Cung thủ.
- **Cấp 21 trở lên:** tốc độ đánh chạm +60%, chỉ máu và sát thương còn tăng; mọi hero chạm cùng lúc nên vẫn cân.

## Đã tự quyết
- **S là căn của tích máu và sát thương.** Hero nhiều máu ít sát thương và hero ít máu nhiều sát thương so được trên cùng thang. Đã loại: cộng máu với sát thương (đơn vị khác nhau).
- **Đòn vùng ×2, tầm từ 5u máu ×2.** Ước lượng thô, đủ để thấy không hero nào lệch hẳn. Đã loại: bỏ qua đòn vùng và tầm (Pháp sư sẽ trông yếu nhất xa).
- **Ngưỡng ±10%.** Đủ chặt để không có hero rõ ràng tốt nhất, đủ lỏng để hero có cá tính.

## Chưa rõ
- Hiệp sĩ có S thấp nhất (60, −8%). Nếu chơi thử thấy ít ai chọn, đề xuất tăng máu Hiệp sĩ từ 300 lên 340 (S thành 63.9).
- Kiếm khách có sát thương mỗi giây cao gần gấp đôi Hiệp sĩ; nếu đội Kiếm khách + Cung thủ + Pháp sư thắng rõ mọi đội khác khi chơi thử, đề xuất giảm sát thương Kiếm khách từ 16 xuống 14 (20 sát thương/giây, S 60.0).
