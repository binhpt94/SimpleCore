# Hai bước của popup tạm dừng

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Mở khi `feature-run/run-pause` nhờ (qua `feature-popup`), luôn ở bước chính.
- Đổi bước trong cùng popup khi chạm "Bỏ lượt" hoặc "Không".
- Báo lựa chọn cuối (tiếp tục hoặc bỏ lượt) cho `feature-run/run-pause` một lần; `feature-run` đóng popup.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số popup tạm dừng mở cùng lúc | tối đa 1 | popup | `feature-run/run-pause/pause-rules` |
| Số bước | 2 (chính, hỏi xác nhận) | bước | Xác nhận trong cùng popup |
| Bước chính | tiêu đề "Tạm dừng"; nút "Tiếp tục" (chính), "Bỏ lượt" | 2 nút | |
| Bước hỏi | câu "Bỏ lượt này? Điểm tính theo số đợt đã dọn."; nút "Bỏ lượt", "Không" (chính) | 2 nút | Nút an toàn là nút chính |
| Đếm ngược khi tiếp tục | 0 | giây | Trận chạy ngay |
| Số lần báo mỗi lần popup mở | 1 | lần | |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Có popup tạm dừng | Không | Không bắt buộc (nếu có: chỉ nút "Tiếp tục") | Có đủ 2 bước |
| Bước hỏi xác nhận | Không | Không bắt buộc | Có |
| Hiệu ứng mở popup | Không | Không | Phóng từ nhỏ ra đủ cỡ trong 0.2 giây (dự kiến) |

## Trường hợp biên
- **Rời game khi popup đang mở (ở bất kì bước nào):** không mở thêm popup; quay lại vẫn đúng bước cũ.
- **Mở lại popup sau khi đã tiếp tục:** luôn bắt đầu ở bước chính, không nhớ bước hỏi.
- **Chạm "Tiếp tục" và "Bỏ lượt" gần như cùng lúc:** chỉ chạm được xử lí trước tính; nếu là "Bỏ lượt" thì sang bước hỏi, chạm kia bị bỏ.
- **Bỏ lượt khi chưa dọn đợt nào:** vẫn hỏi xác nhận; điểm 0.
- **Nút quay lại của máy bấm liên tục:** bước hỏi → bước chính → tiếp tục; mỗi lần bấm một bước.

## Đã tự quyết
- **Ở bước hỏi, nút chính là "Không".** Bấm nhầm thì an toàn. Đã loại: "Bỏ lượt" là nút chính.
- **Câu hỏi nói rõ điểm vẫn tính.** Người chơi biết bỏ lượt không mất các đợt đã dọn.
- **Nút quay lại của máy = tiếp tục ở bước chính.** Thói quen "quay lại là đóng" trên điện thoại; không bao giờ dẫn tới bỏ lượt.

## Chưa rõ
- Không có điểm chặn.
