# Một yêu cầu quảng cáo và kết quả của nó

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lần đề nghị (ví dụ một đề nghị hồi sinh) sinh nhiều nhất MỘT yêu cầu quảng cáo.
- Một yêu cầu có đúng MỘT kết quả.
- Bên gọi hiện tại: `feature-popup-revive`. Bên gọi mới về sau dùng cùng đường này.
- Feature này nói chuyện với nhà cung cấp quảng cáo; feature khác không nói trực tiếp.

## Định nghĩa
- Không dùng thuật ngữ riêng.

## Danh sách
Các bước của một yêu cầu:
| Bước | Kết thúc khi |
|---|---|
| Chờ mở | Quảng cáo bắt đầu phát, hoặc quá hạn, hoặc nhà cung cấp báo không có |
| Đang phát | Quảng cáo đóng |
| Chờ xác nhận thưởng | Nhà cung cấp xác nhận đủ điều kiện, hoặc quá hạn |
| Có kết quả | Đã trả kết quả cho bên gọi (sau khi game trở lại màn hình) |

Kết quả: "đã xem đủ" hoặc "không xem được".

## Luật cơ bản
- Một yêu cầu một lúc; yêu cầu chen vào nhận ngay "không xem được".
- Chỉ "đã xem đủ" khi nhà cung cấp xác nhận đủ điều kiện nhận thưởng.
- Bên gọi huỷ thì kết quả tới sau bị bỏ.
- Game đứng yên trong lúc quảng cáo phát là việc của bên gọi (`feature-run`), không phải của feature này.

## Đã tự quyết
- **Chỉ hai kết quả.** Bên gọi chỉ cần biết có thưởng hay không; phân biệt lí do lỗi là việc của log. Đã loại: trả mã lỗi chi tiết cho bên gọi.
- **Một yêu cầu một lúc, không hàng chờ.** Game chỉ có một chỗ xem quảng cáo. Đã loại: xếp hàng yêu cầu.

## Chưa rõ
- Độ dài hạn chờ: vòng 4. Nhà cung cấp quảng cáo: phiên code cùng @CodeLead.
