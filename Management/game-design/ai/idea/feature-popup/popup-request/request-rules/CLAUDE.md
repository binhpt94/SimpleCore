# Luật mở, đóng popup và danh sách id

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Bước tải của scene Game: dịch vụ chuẩn bị mọi popup có id trong danh sách của scene (asset nằm ở `FeatureAssets/Public` của feature sở hữu). Thiếu asset của một id thì bước tải lỗi, nêu id (`feature-session-scene/session-steps/participant-order`).
- Feature gọi xin mở bằng id: dịch vụ đặt popup lên trên cùng (`feature-popup/popup-layer/stack-rules`) và trả popup cho feature gọi điền nội dung, nghe nút.
- Feature gọi xin đóng: popup và nền che của nó biến mất.
- Bước huỷ của scene: mọi popup đang mở và mọi popup đã chuẩn bị bị bỏ.

Danh sách id của scene Game:

| Id | Feature sở hữu | Số nút | Nút | Chặn gì |
|---|---|---|---|---|
| `PopupLose/Main` | `feature-popup-lose` | 2 | Chơi lại; Về màn bắt đầu | Mọi thứ bên dưới (trận đã dừng) |
| `PopupPause/Main` | `feature-popup-pause` | 2 mỗi bước | Bước chính: Tiếp tục; Bỏ lượt. Bước hỏi: Bỏ lượt; Không | Mọi thứ bên dưới (trận đứng yên) |

Nội dung chữ, số trên popup do feature sở hữu quyết (`feature-popup-lose/lose-result`, `feature-popup-pause/pause-popup`); dịch vụ không biết.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số id popup scene Game | 2 | id | Theo các feature popup hiện có |
| Thời gian từ lúc xin mở tới lúc popup hiện | 1 | khung hình | Đã chuẩn bị ở bước tải |
| Số lần bấm nút được xử lí mỗi lần mở | 1 | lần | Chặn bấm đúp |
| Popup trong scene Loading | 0 | popup | Loading không có popup |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Số id popup scene Game | 2 | 2 (thêm nếu có popup mới) | Theo danh sách popup lúc đó |
| Thời gian từ xin mở tới hiện | 1 khung hình | 1 khung hình (hiệu ứng mở chạy sau đó) | Như PROTOTYPE |

## Trường hợp biên
- **Id có nhưng asset thiếu**: lỗi ngay ở bước tải, scene Game không chạy; không chờ tới lúc mở mới báo.
- **Id sai dạng (không có `/`, thiếu `Owner` hay `Name`)**: lỗi ngay ở bước tải, nêu id.
- **Hai feature khai cùng một id**: lỗi ở bước tải, nêu id trùng.
- **Xin mở id đang mở**: từ chối (`same-id-once` ở `feature-popup/popup-layer/stack-rules`).
- **Xin đóng popup đã đóng hoặc chưa từng mở**: bỏ qua, không lỗi.
- **Bấm nút ngay rồi bấm tiếp nút khác trước khi popup đóng**: feature gọi chỉ xử lí lần đầu.
- **Đang mở popup tạm dừng mà lượt kết thúc (không xảy ra vì trận đứng yên)**: nếu vẫn xảy ra, feature gọi đóng popup tạm dừng rồi mới mở popup thua.
- **Rời scene khi có popup mở**: popup bị huỷ, không báo bấm nút; feature gọi không nhận gì thêm từ popup đó.
- **Feature gọi xin mở trước khi scene vào bước chạy**: từ chối, ghi lỗi; popup chỉ mở được khi scene đang chạy.

## Đã tự quyết
- **Chuẩn bị sẵn ở bước tải.** Popup thua phải hiện ngay khi thành bị phá; chờ tải giữa trận dễ thấy giật. Đã loại: tải lúc xin mở.
- **Hộp xác nhận bỏ lượt là bước trong popup tạm dừng, không có id riêng (2026-11-23, sửa lệch).** Bản cũ khai `PopupPause/QuitConfirm`, trái với `feature-popup-pause/pause-popup` (ý `popup-quit-confirm`: xác nhận ngay trong popup) và node con `pause-popup-spec` của nó. Feature sở hữu quyết nội dung popup của mình. Đã loại: popup riêng chồng lên.
- **Chặn bấm đúp ở feature gọi.** Dịch vụ không biết nút nào là nút; giữ dịch vụ không biết nội dung popup. Đã loại: dịch vụ tự khoá popup sau lần bấm đầu.
- **Rời scene không báo bấm nút.** Tránh feature gọi làm việc (chơi lại, chuyển màn) khi scene đang huỷ.

## Chưa rõ
- Tên `Name` của từng popup do `feature-popup-lose` và `feature-popup-pause` chốt; đổi tên thì sửa bảng này.
