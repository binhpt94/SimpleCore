# Đội nhớ và đội mặc định

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ một đội nhớ (3 hero theo thứ tự chỗ) trong lúc game đang mở; ban đầu chưa có.
- Khi màn chọn đội mở: có đội nhớ thì đổ vào 3 chỗ; chưa có thì đổ đội mặc định.
- Khi bấm "Vào trận": ghi đội đang chọn thành đội nhớ.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số đội nhớ | 1 | đội | `memory-which-team` |
| Đội mặc định | Hiệp sĩ (chỗ 1), Đấu sĩ rìu (chỗ 2), Cung thủ (chỗ 3) | | 2 cận chiến chắn và dọn đông, 1 bắn xa |
| Lúc ghi đội nhớ | khi bấm "Vào trận" | | Luôn là đội đủ đã từng đánh |
| Lưu khi tắt game | không | | Chỉ kỉ lục được lưu |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Đội vào trận | Luôn là đội mặc định (chưa có màn chọn đội) | Đội nhớ hoặc đội mặc định đổ sẵn, người chơi đổi được | Như PROTOTYPE |
| Đội mặc định | Hiệp sĩ, Đấu sĩ rìu, Cung thủ | Như IDEA | Như IDEA (chỉnh theo `feature-balance` nếu cần) |

## Trường hợp biên
- **Đổi đội ở màn chọn đội:** màn này chỉ rời được bằng "Vào trận", nên đội đã đổi thành đội nhớ ngay khi vào trận. Tắt game giữa lúc đổi thì đội nhớ cũ cũng mất.
- **Đội nhớ có hero không còn trong danh sách (hero bị bỏ khỏi `feature-hero`):** chỗ đó để trống, người chơi chọn thêm cho đủ 3.
- **Đội mặc định có hero không còn trong danh sách:** chỗ đó để trống như trên.

## Đã tự quyết
- **Có đội mặc định thay vì để trống.** Trả lời câu "Chưa rõ" của node cha. Người mới vào trận bằng hai chạm (Chơi, Vào trận). Đội mặc định trùng đội IDEA ở `feature-run/run-start/start-values`. Đã loại: để trống; đội ngẫu nhiên (khó kiểm, khó cân bằng lần đầu).
- **Giữ thứ tự chỗ.** Thứ tự không ảnh hưởng trận nhưng giữ để thanh nâng cấp hero (`feature-hud-upgrade`, xếp theo thứ tự đội) không đổi chỗ giữa các lượt.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ trường hợp biên bỏ lượt rồi bấm "Chơi" lại.

## Chưa rõ
- Không có điểm chặn.
