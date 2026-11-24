# Giá trị đầu lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Khi trận được dựng: nhận đội 3 hero từ `feature-team-select`, báo "lượt mới" kèm đội cho các feature trong trận.
- Mỗi feature tự đặt giá trị của mình theo bảng dưới (giá trị nằm trong cấu hình của `feature-balance`); feature này không giữ hộ.
- Chỉ cho `feature-wave` bắt đầu đợt 1 sau khi mọi feature đã đặt xong.

## Thông số
| Thứ | Giá trị đầu lượt | Đơn vị | Feature giữ | Lí do |
|---|---|---|---|---|
| Máu thành | 1000 / 1000 (dự kiến) | máu | `feature-castle` | Bộ số chung |
| Cấp nâng cấp thành (máu, sức đánh, kĩ năng) | 1 | cấp | `feature-upgrade` | Bắt đầu lại từ đầu |
| Tiền | 50 (dự kiến) | xu | `feature-wallet` | Bộ số chung; đủ mua vài nâng cấp đầu |
| Cấp từng hero | 1 | cấp | `feature-upgrade` | Bắt đầu lại từ đầu |
| Đợt hiện tại | 1 | đợt | `feature-wave` | Chưa dọn đợt nào |
| Điểm | 0 | đợt | `feature-run/run-score` | |
| Kĩ năng thành | cả 3 sẵn sàng, hồi chiêu 0 | giây | `feature-castle` | `feature-castle/castle-skills` |
| Hero trên chiến trường | 3, đủ máu, ở thành | hero | `feature-hero` | Đội 3 hero |
| Kỉ lục | giữ nguyên | đợt | `feature-run/run-score` | Thứ duy nhất không đặt lại |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Đội vào trận | Đội mặc định cố định (Hiệp sĩ, Đấu sĩ rìu, Cung thủ) | Đội vừa chọn ở màn chọn đội | Đội vừa chọn ở màn chọn đội |
| Máu thành đầu lượt | 1000 | 1000 (chỉnh theo thử) | Theo `feature-balance` |
| Tiền đầu lượt | 50 | 50 (chỉnh theo thử) | Theo `feature-balance` |
| Lần hồi sinh thành đầu lượt | Không có (chưa có hồi sinh) | 1 | 1 |

## Trường hợp biên
- **Đội nhận được không đủ 3 hero hoặc có hero trùng:** không vào trận; đây là lỗi của màn chọn đội, báo lỗi rõ cho phiên code (người chơi không gặp vì nút vào trận bị khoá).
- **Chơi lại ngay sau thua:** mọi giá trị về như bảng, kể cả máu tối đa đã được nâng ở lượt trước.
- **Hiệu ứng kĩ năng còn dở từ lượt trước (ví dụ làm chậm):** bị huỷ cùng trận cũ, không mang sang.
- **Tiền thưởng đợt hay tiền hạ quái tới sau khi lượt cũ kết thúc:** bỏ qua, không cộng vào lượt mới.

## Đã tự quyết
- **Đội mặc định ở IDEA là Hiệp sĩ, Đấu sĩ rìu, Cung thủ.** Đủ hai kiểu cận chiến (chắn, dọn đông) và một bắn xa; trùng đội mặc định của `feature-team-select/team-memory/memory-rules`. Đã loại: 3 cận chiến (không thấy hero bắn xa).
- **Đợt 1 chỉ bắt đầu sau khi đặt xong mọi giá trị.** Khoảng chờ trước đợt 1 dài bao lâu là việc của `feature-wave`.

## Chưa rõ
- Khoảng chờ trước khi đợt 1 ra quái do `feature-wave` quyết (gợi ý bằng thời gian nghỉ 3 giây giữa hai đợt).
