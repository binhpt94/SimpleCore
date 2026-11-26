# Thành phần màn bắt đầu

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Hiện màn bắt đầu trong scene Game khi `feature-run/run-flow/flow-rules` yêu cầu.
- Mỗi lần hiện: đọc kỉ lục từ `feature-run/run-score/score-rules`, ghi vào dòng kỉ lục.
- Nút "Chơi": báo `feature-run` "đã bấm chơi"; không tự chuyển màn.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số thành phần | 3 (tên game, kỉ lục, nút Chơi) | | `home-items` |
| Số nút | 1 | nút | Vào trận bằng một chạm |
| Chữ tên game | "Thủ thành anh hùng" (tên tạm) | | Node gốc |
| Chữ kỉ lục | "Kỉ lục: N đợt" | | Kỉ lục là số đợt dọn sạch |
| Kỉ lục khi chưa có | 0 | đợt | `record-default-zero` |
| Chữ trên nút | "Chơi" | | |
| Số lần chạm có tác dụng mỗi lần màn hiện | 1 | lần | Tránh chuyển màn hai lần |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Có màn bắt đầu | Không (mở game vào thẳng trận) | Có, 3 thành phần dạng chữ và nút đơn giản | Có, kèm hình nền và logo tên game |
| Hiệu ứng khi màn hiện | Không | Không | Tên game và nút hiện dần, 0.3 giây (dự kiến) |

## Trường hợp biên
- **Kỉ lục rất lớn (ví dụ 999):** hiện đủ số, không rút gọn.
- **Về màn bắt đầu bằng bỏ lượt và vừa phá kỉ lục:** dòng kỉ lục hiện số mới; màn này không báo "kỉ lục mới" (chỉ popup thua báo).
- **Nút quay lại của máy:** không làm gì (`feature-run/run-flow/flow-rules`).
- **Màn hiện lại nhiều lần trong một lần mở game:** nút "Chơi" lại nhận một lần chạm mới mỗi lần màn hiện.

## Đã tự quyết
- **Chữ "Kỉ lục: N đợt" kể cả khi N = 0.** Người chơi mới thấy luôn mục tiêu là số đợt. Đã loại: ẩn dòng kỉ lục khi chưa chơi.
- **Không báo kỉ lục mới ở màn này.** Tránh trùng với `feature-popup-lose`.

## Chưa rõ
- Tên game chính thức chưa có; dùng tên tạm.
