# Mức tiền thu

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Khi `feature-enemy` báo một quái bị hạ (máu về 0), kèm mức tiền của loại quái đó: ví cộng đúng mức đó (`feature-wallet/balance-spend/wallet-rules`).
- Khi `feature-wave` báo một đợt được dọn sạch, kèm số đợt: cộng 20 + 5 × số đợt.
- Thứ tự trong một khung hình con quái cuối của đợt gục: cộng tiền con quái, rồi cộng thưởng dọn đợt.
- Bảng tiền theo loại quái nằm trong số liệu của `feature-enemy`; công thức thưởng nằm trong số liệu của `feature-wave`. Cả hai là asset cấu hình, không viết cứng.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Tiền hạ Sói | 4 (dự kiến) | tiền/con | Yếu, nhanh, đi đông |
| Tiền hạ Lính | 5 (dự kiến) | tiền/con | Quái cơ bản, mốc so sánh |
| Tiền hạ Cung thủ quái | 6 (dự kiến) | tiền/con | Đánh xa, khó hạ hơn Lính |
| Tiền hạ Khổng lồ | 15 (dự kiến) | tiền/con | Trâu, ~3 lần Lính |
| Tiền hạ Trùm | 100 (dự kiến) | tiền/con | Một lần hạ đủ mua 1-2 cấp |
| Thưởng dọn đợt | 20 + 5 × n (dự kiến) | tiền, n = số đợt vừa dọn | Đợt sau khó hơn thì thưởng lớn hơn |
| Nhân theo đợt cho tiền hạ quái | không | | Số quái tăng đã làm thu nhập tăng |

Ví dụ thưởng dọn đợt: đợt 1 = 25, đợt 3 = 35, đợt 5 = 45, đợt 10 = 70, đợt 20 = 120.

Dự đoán cân bằng (dự kiến, để `feature-balance` kiểm): một đợt đầu khoảng 8 Lính cho 40 + thưởng 25 = 65 tiền, đủ khoảng 2 lần mua cấp thấp. Thưởng tăng tuyến tính còn giá tăng theo cấp số nhân, nên càng về sau mỗi đợt mua được ít cấp hơn: đó là cách game khó dần.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Loại quái có tiền | Mọi loại `feature-enemy` có ở mức IDEA, theo bảng trên | Đủ 5 loại | Đủ 5 loại, chỉnh theo chơi thử |
| Thưởng dọn đợt | 20 + 5 × n | 20 + 5 × n | Chốt theo `feature-balance` |
| Số tiền nổi lên chỗ quái gục | không | có (chỉ hiển thị, tiền vẫn vào ngay) | có |

## Trường hợp biên
- **Hai đòn trúng cùng lúc làm một quái gục**: chỉ một lần cộng.
- **Quái gục bởi Sấm sét, đòn tự bắn hay hero**: cùng mức tiền.
- **Quái gục cùng khung hình thành bị phá**: ví vẫn cộng; không ảnh hưởng vì lượt kết thúc và lượt sau đặt lại.
- **Đợt cuối được dọn cùng lúc thành bị phá**: thưởng có thể được cộng hay không đều không ảnh hưởng gì (tiền mất khi hết lượt); `feature-run` quyết thua trước.
- **Quái bị xoá khi lượt kết thúc hoặc khi rời màn**: 0 tiền.
- **Quái gục trong lúc nghỉ giữa đợt** (sót lại): vẫn được tiền; thưởng dọn đợt chỉ một lần cho mỗi đợt.
- **Trùm**: 100 tiền, không nhận thêm thưởng riêng ngoài thưởng dọn đợt.

## Đã tự quyết
- **Không nhân tiền hạ quái theo đợt.** Theo bộ số chung; đợt sau đông quái hơn đã đủ tăng thu. Đã loại: nhân hệ số theo đợt (thu tăng quá nhanh so với độ khó).
- **Tiền con quái cuối cộng trước thưởng đợt.** Thứ tự cố định để test được. Đã loại: thứ tự không xác định.
- **Chỉ "bị hạ" mới có tiền.** Tránh tiền ma khi xoá quái. Đã loại: mọi lần quái biến mất đều có tiền.

## Chưa rõ
- Đề xuất cho người: Sói 4 tiền rẻ hơn Lính trong khi đi nhanh và đông; nếu Sói thực tế khó hạ hơn Lính thì nên ngang Lính (5). Hiện vẫn dùng 4.
- Số đợt có Trùm và số quái mỗi đợt là của `feature-wave`; dự đoán cân bằng ở trên giả định đợt 1 có khoảng 8 Lính.
