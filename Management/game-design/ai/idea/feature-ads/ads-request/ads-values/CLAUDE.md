# Thông số yêu cầu quảng cáo

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Nhận yêu cầu kèm mã đề nghị; từ chối ngay nếu đang bận.
- Chờ mở (tối đa 8 giây), cho phát, chờ xác nhận thưởng (tối đa 5 giây), chờ game trở lại, rồi trả kết quả kèm mã.
- Bên gọi huỷ thì bỏ kết quả.
- Ghi lí do của mọi "không xem được" vào log.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Hạn chờ mở | 8 (dự kiến) | giây | Mạng chậm vẫn kịp; người chơi đang chờ |
| Hạn chờ xác nhận thưởng | 5 (dự kiến) | giây | Xác nhận thường tới ngay khi đóng |
| Thời gian quảng cáo đang phát | không giới hạn, không tính hạn | | Do nhà cung cấp |
| Số yêu cầu cùng lúc | 1 | yêu cầu | Node cha |
| Màn chờ của nhà cung cấp giả | 2 | giây | Đủ thấy trạng thái chờ của popup |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Quảng cáo | Không có | Nhà cung cấp giả (bản dev và bản thử) | Nhà cung cấp thật, nhà cung cấp giả chỉ ở bản dev |
| Hạn chờ mở, xác nhận | | 8 giây, 5 giây | Chỉnh theo nhà cung cấp thật |
| Log lí do | | Có | Có |

## Trường hợp biên
- **Mất mạng giữa lúc quảng cáo phát:** chờ quảng cáo đóng; không có xác nhận trong 5 giây thì "không xem được".
- **Xác nhận thưởng tới trước khi quảng cáo đóng:** giữ, trả "đã xem đủ" sau khi đóng và game trở lại.
- **Người chơi tắt game khi quảng cáo phát:** không có kết quả; lượt dở không lưu (chỉ kỉ lục lưu).
- **Kết quả tới khi bên gọi đã huỷ:** bỏ, ghi log.

## Đã tự quyết
- **Có nhà cung cấp giả.** Chọn nhà cung cấp thật là việc của phiên code và @CodeLead; luồng hồi sinh phải thử được trước đó. Đã loại: chờ có nhà cung cấp thật mới làm.
- **Không có hạn khi chờ game trở lại.** Người chơi đang ở ứng dụng khác; trận vẫn đứng yên.

## Chưa rõ
- Nhà cung cấp quảng cáo thật: phiên code cùng @CodeLead (không chặn PROTOTYPE vì có nhà cung cấp giả).
