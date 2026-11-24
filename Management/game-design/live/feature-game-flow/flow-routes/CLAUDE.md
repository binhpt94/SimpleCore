# Các màn chính và đường đi giữa chúng

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một scene Game có BA màn chính; mỗi lúc MỘT màn hiện.
- `feature-scene-flow` báo scene Game sẵn sàng; điều hướng màn mở màn bắt đầu.
- `feature-home`, `feature-team-select` gửi yêu cầu thẳng.
- `feature-popup-lose`, `feature-popup-pause` báo lựa chọn cho `feature-run`; `feature-run` dọn trận rồi chuyển lựa chọn tới đây.
- `feature-run` nhận đội khi vào trận, báo khi trận đã dựng xong.

## Định nghĩa
- `glossary#screen`

## Danh sách
| Từ | Yêu cầu | Ai gửi | Tới |
|---|---|---|---|
| (scene Game sẵn sàng) | | `feature-scene-flow` | Màn bắt đầu |
| Màn bắt đầu | Chơi | `feature-home` | Màn chọn đội |
| Màn chọn đội | Vào trận | `feature-team-select` | Màn trận (khi trận dựng xong) |
| Màn chọn đội | Quay lại | `feature-team-select` | Màn bắt đầu |
| Màn trận | Chơi lại (popup thua) | `feature-run` sau khi dọn trận | Màn chọn đội |
| Màn trận | Về màn bắt đầu (popup thua) | `feature-run` sau khi dọn trận | Màn bắt đầu |
| Màn trận | Bỏ lượt (popup tạm dừng) | `feature-run` sau khi dọn trận | Màn bắt đầu |

## Luật cơ bản
- Mỗi lúc đúng một màn chính.
- Đang đổi màn thì bỏ yêu cầu mới.
- Rời trận luôn đi qua `feature-run` để dọn trận trước.
- Màn trận chỉ hiện khi trận đã dựng xong.

## Đã tự quyết
- **Thêm nút "Quay lại" ở màn chọn đội (theo ý `back-from-team` của vòng 2).** Nhánh nhớ đội của `feature-team-select` trước ghi không có nút này; khi viết vòng 4 của điều hướng màn sẽ sửa màn chọn đội cho khớp.
- **Bảng đường đi chuyển từ lượt chơi về đây (ý `takes-over-run-routes`).** Nhánh luồng màn của `feature-run` đang ghi "bên duy nhất quyết định sang màn nào" và đang được code ở IDEA. Ở IDEA chỉ có màn trận, nên giữ nguyên nhánh đó; khi viết vòng 4 của điều hướng màn (trước PROTOTYPE) sẽ sửa nhánh luồng màn của lượt chơi chỉ còn phần trong trận. Đã loại: sửa ngay bây giờ (đổi code Run đang chạy ở IDEA mà không được gì).

## Chưa rõ
- Hiệu ứng đổi màn và thời gian khoá: vòng 4.
- Chỉnh nhánh luồng màn của `feature-run` và màn chọn đội cho khớp: khi viết vòng 4.
