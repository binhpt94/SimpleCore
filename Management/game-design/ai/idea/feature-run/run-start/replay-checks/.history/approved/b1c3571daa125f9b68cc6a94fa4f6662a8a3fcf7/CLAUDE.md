# Kiểm tra chơi lại nhiều lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Mỗi lần lượt chơi báo lượt mới bắt đầu trong scene Game: các feature trận tự về trạng thái ban đầu theo `feature-run/run-start/start-values`; không feature nào tải lại thứ đã tải, không chuyển scene.
- Đầu mỗi lượt mới phải đúng: không còn quái, hero, hiệu ứng đang chạy của lượt trước, không còn popup mở.
- Kiểm rò rỉ: đọc số view và số việc đang chạy của các feature trận (bộ đếm của `feature-session-scene`) ở đầu lượt 1 và đầu lượt 10; hai số phải bằng nhau.
- Thứ còn lại qua các lượt trong scene Game: kỉ lục, đội đã nhớ. Thứ còn lại sau khi tắt game: chỉ kỉ lục.
- Feature trận được kiểm: `feature-castle`, `feature-wallet`, `feature-upgrade`, `feature-wave`, `feature-hero`, `feature-enemy`.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số lượt liền để kiểm rò rỉ | 10 | lượt | Đủ để thấy thứ dồn lên mỗi lượt |
| Chênh số view, số việc đầu lượt 10 so với đầu lượt 1 | 0 | view, việc | Lượt mới bắt đầu sạch |
| Popup mở ở đầu lượt mới | 0 | popup | Popup thua đã đóng trước khi lượt mới bắt đầu |
| Thời gian từ bấm chơi lại tới màn chọn đội | dưới 0,5 (dự kiến) | giây | Không tải lại scene nên gần như tức thì |
| Thứ được lưu khi tắt game | 1 (kỉ lục) | thứ | `only-record-saved` |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Số lượt liền để kiểm rò rỉ | 3 (kiểm bằng tay khi Play) | 10 | 10, có kiểm bộ nhớ không tăng |
| Thời gian từ bấm chơi lại tới màn chọn đội | dưới 0,5 giây (dự kiến) | dưới 0,5 giây | dưới 0,5 giây |

## Trường hợp biên
- **Lượt kết thúc lúc đang có hiệu ứng (kĩ năng thành, quái chết dở)**: hiệu ứng của lượt cũ bị dừng và bỏ khi lượt mới bắt đầu, không chạy tiếp sang lượt mới.
- **Lượt mới bắt đầu mà còn popup mở (lỗi)**: ghi lỗi nêu id popup; không tự đóng âm thầm.
- **Tiền, nâng cấp, đợt, máu thành của lượt trước**: về giá trị ban đầu của từng feature (`feature-run/run-start/start-values`), không còn sót.
- **Tắt game giữa trận**: chỉ kỉ lục đã lưu trước đó còn lại; lượt dở không được lưu.

## Đã tự quyết
- **Tách từ node kiểm tra dọn sạch của `feature-session-scene` (2026-11-25, người chọn hướng A).** Ba ý `replay-no-growth`, `popup-cleared-on-reset`, `only-record-saved` và các trường hợp biên về đặt lại lượt chuyển sang đây, giữ nguyên id và nội dung. Đặt lại giữa hai lượt là việc của lượt chơi (`run-reset-in-scene` ở nhánh dọn sạch của `feature-session-scene`), không qua vòng đời scene. Đã loại: giữ ở dịch vụ vòng đời scene (nó không có code cho các luật này).
- **Lượt chơi ghi lỗi popup sót, không tự đóng.** Theo luật "không âm thầm bỏ qua"; popup sót là lỗi của feature gọi.
- **Đội đã nhớ không lưu khi tắt game.** Theo `feature-team-select/team-memory`.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ trường hợp biên bỏ lượt từ popup tạm dừng.

## Chưa rõ
- Không có điểm chặn.
