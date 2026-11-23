# Kiểm tra dọn sạch và chơi lại nhiều lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Sau bước huỷ của một scene: đếm view và việc đang chạy (hẹn giờ, việc chờ, việc lặp mỗi khung hình) của mọi feature tham gia; phải bằng 0. Lớp che của `feature-scene-flow` không tính.
- Trong scene Game, mỗi lần `feature-run` báo lượt mới bắt đầu: các feature trận tự về trạng thái ban đầu (`feature-run/run-start`); không feature nào tải lại thứ đã tải ở bước tải, không chuyển scene.
- Đầu mỗi lượt mới phải đúng: không còn quái, hero của lượt trước, không còn popup mở, không còn hiệu ứng đang chạy của lượt trước.
- Thứ còn lại qua các lượt trong scene Game: kỉ lục, đội đã nhớ. Thứ còn lại sau khi tắt game: chỉ kỉ lục.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| View còn lại sau bước huỷ | 0 | view | `nothing-survives` |
| Việc đang chạy còn lại sau bước huỷ | 0 | việc | `nothing-survives` |
| Số lượt liền để kiểm rò rỉ | 10 | lượt | Đủ để thấy thứ dồn lên mỗi lượt |
| Chênh số view đầu lượt 10 so với đầu lượt 1 | 0 | view | Lượt mới bắt đầu sạch |
| Popup mở ở đầu lượt mới | 0 | popup | Popup thua đã đóng trước khi lượt mới bắt đầu |
| Thời gian từ bấm chơi lại tới màn chọn đội | dưới 0,5 (dự kiến) | giây | Không tải lại scene nên gần như tức thì |
| Thứ được lưu khi tắt game | 1 (kỉ lục) | thứ | `saved-data-only` |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Số lượt liền để kiểm rò rỉ | 3 (kiểm bằng tay khi Play) | 10 | 10, có kiểm bộ nhớ không tăng |
| Thời gian từ bấm chơi lại tới màn chọn đội | dưới 0,5 giây (dự kiến) | dưới 0,5 giây | dưới 0,5 giây |
| Kiểm sau bước huỷ | Không bắt buộc (chỉ một lần chuyển, Loading → Game) | Đếm view và việc đang chạy | Như PROTOTYPE |

## Trường hợp biên
- **Bỏ lượt từ popup tạm dừng**: popup xác nhận và popup tạm dừng đều đóng trước khi về màn bắt đầu; trận đứng yên lúc tạm dừng vẫn phải được dọn khi lượt mới bắt đầu.
- **Lượt kết thúc lúc đang có hiệu ứng (kĩ năng thành, quái chết dở)**: hiệu ứng của lượt cũ bị dừng và bỏ khi lượt mới bắt đầu, không chạy tiếp sang lượt mới.
- **Lượt mới bắt đầu mà còn popup mở (lỗi)**: ghi lỗi nêu id popup; không tự đóng âm thầm.
- **Tiền, nâng cấp, đợt, máu thành của lượt trước**: về giá trị ban đầu của từng feature (`feature-run/run-start`), không còn sót.
- **Tắt game giữa trận**: chỉ kỉ lục đã lưu trước đó còn lại; lượt dở không được lưu.

## Đã tự quyết
- **Kiểm bằng số đếm, không bằng mắt.** Đếm view và việc đang chạy thì kiểm được. Đã loại: "chơi lại vẫn mượt" (không kiểm được).
- **Đội đã nhớ không lưu khi tắt game.** Theo `feature-team-select/team-memory` (`memory-session-only`).
- **Không tự đóng popup sót.** Theo luật "không âm thầm bỏ qua"; popup sót là lỗi của feature gọi.

## Chưa rõ
- Không có điểm chặn.
