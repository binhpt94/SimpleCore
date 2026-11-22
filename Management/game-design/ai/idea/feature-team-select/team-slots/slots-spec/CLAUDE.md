# Ba chỗ trong đội và nút vào trận

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ 3 chỗ, mỗi chỗ một hero hoặc trống. Lúc mở màn, chỗ được đổ từ `feature-team-select/team-memory/memory-rules`.
- Nhận chạm từ thẻ (`feature-team-select/hero-cards/card-spec`) và từ chỗ; áp luật thêm, bỏ.
- Nút "Vào trận": khoá khi thiếu, mở khi đủ 3; bấm thì ghi đội nhớ và báo đội cho `feature-run/run-start`.
- Mỗi chỗ hiện hình và tên hero đang ở đó, hoặc trống.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số chỗ | 3 | chỗ | Bộ số chung; 5 hero bỏ lại 2 là có lựa chọn |
| Số hero chọn được | 5 | hero | `feature-hero/hero-list` |
| Số nút | 1 ("Vào trận") | nút | |
| Số lần một hero ở trong đội | tối đa 1 | lần | Không trùng |
| Số cách đội có thể ghép | 10 | đội | Chọn 3 trong 5, không tính thứ tự |
| Số chạm có tác dụng của nút "Vào trận" mỗi lần màn mở | 1 | lần | Tránh vào trận hai lần |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Màn chọn đội | Không có; đội cố định Hiệp sĩ, Đấu sĩ rìu, Cung thủ | Có đủ luật chọn | Có đủ luật chọn |
| Số chỗ | 3 | 3 | 3 (chỉnh qua `feature-balance` nếu cần) |
| Hiệu ứng hero vào chỗ | Không | Không | Hình hero bay từ thẻ vào chỗ, 0.2 giây (dự kiến) |

## Trường hợp biên
- **Chạm cùng lúc hai thẻ khi còn một chỗ:** thẻ được xử lí trước vào chỗ; thẻ sau bị bỏ qua vì đội đã đủ.
- **Chạm chỗ trống:** không làm gì.
- **Bỏ hero ra rồi chọn lại chính hero đó:** vào chỗ trống có số nhỏ nhất, có thể khác chỗ cũ.
- **Đội nhớ đổ vào thiếu chỗ (hero không còn trong danh sách):** nút "Vào trận" khoá tới khi đủ 3.
- **Nút quay lại của máy:** không làm gì.

## Đã tự quyết
- **Vào chỗ trống số nhỏ nhất.** Dễ đoán, kiểm được. Đã loại: luôn vào chỗ cuối.
- **Bỏ hero không dồn chỗ.** Hero khác không nhảy chỗ bất ngờ.
- **Chạm cả thẻ lẫn chỗ đều bỏ được.** Một chạm, chỗ nào tiện thì chạm.

## Chưa rõ
- Không có điểm chặn.
