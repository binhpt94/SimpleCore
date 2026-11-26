# Luật mở, đóng popup và danh sách id

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Bước tải của scene Game: dịch vụ chuẩn bị mọi popup có id trong danh sách của scene (asset nằm ở `FeatureAssets/Public` của feature sở hữu). Thiếu asset của một id thì bước tải lỗi, nêu id (`feature-session-scene/session-steps/participant-order`).
- Feature gọi xin mở bằng id: dịch vụ đặt popup lên trên cùng (`feature-popup/popup-layer/stack-rules`) và trả popup cho feature gọi điền nội dung, nghe nút.
- Feature gọi xin đóng: popup và nền che của nó biến mất.
- Bước huỷ của scene: mọi popup đang mở và mọi popup đã chuẩn bị bị bỏ.

Danh sách id của scene Game (đủ mọi giai đoạn; id nào có ở giai đoạn nào xem bảng "Thông số theo giai đoạn"):

| Id | Feature sở hữu | Số nút | Nút | Chặn gì |
|---|---|---|---|---|
| `PopupLose/Main` | `feature-popup-lose` | 2 | Chơi lại; Về màn bắt đầu | Mọi thứ bên dưới (trận đã dừng) |
| `PopupRating/Main` | `feature-popup-rating` | 2 | Đánh giá; Để sau | Mọi thứ bên dưới, kể cả popup thua |

Nội dung chữ, số trên popup do feature sở hữu quyết (`feature-popup-lose/lose-result`); dịch vụ không biết.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số id popup scene Game | 2 | id | Theo các feature popup có trong scene Game |
| Thời gian từ lúc xin mở tới lúc popup hiện | 1 | khung hình | Đã chuẩn bị ở bước tải |
| Số lần bấm nút được xử lí mỗi lần mở | 1 | lần | Chặn bấm đúp |
| Popup trong scene Loading | 0 | popup | Loading không có popup |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Số id popup scene Game | 2: `PopupLose/Main`, `PopupRating/Main` | 2: `PopupLose/Main`, `PopupRating/Main` | Theo danh sách popup lúc đó |
| Thời gian từ xin mở tới hiện | 1 khung hình | 1 khung hình (hiệu ứng mở chạy sau đó) | Như PROTOTYPE |

## Trường hợp biên
- **Id có nhưng asset thiếu**: lỗi ngay ở bước tải, scene Game không chạy; không chờ tới lúc mở mới báo.
- **Id sai dạng (không có `/`, thiếu `Owner` hay `Name`)**: lỗi ngay ở bước tải, nêu id.
- **Hai feature khai cùng một id**: lỗi ở bước tải, nêu id trùng.
- **Xin mở id đang mở**: từ chối (`same-id-once` ở `feature-popup/popup-layer/stack-rules`).
- **Xin đóng popup đã đóng hoặc chưa từng mở**: bỏ qua, không lỗi.
- **Bấm nút ngay rồi bấm tiếp nút khác trước khi popup đóng**: feature gọi chỉ xử lí lần đầu.
- **Rời scene khi có popup mở**: popup bị huỷ, không báo bấm nút; feature gọi không nhận gì thêm từ popup đó.
- **Feature gọi xin mở trước khi scene vào bước chạy**: từ chối, ghi lỗi; popup chỉ mở được khi scene đang chạy.

## Đã tự quyết
- **Ở IDEA chỉ có `PopupLose/Main` (2026-11-25, sửa lệch, theo @Manager).** Ý `id-list` bản trước có cả `PopupPause/Main`, trong khi IDEA không có popup tạm dừng (`feature-popup-pause/pause-popup`, bảng giai đoạn của node con) và popup tạm dừng không tham gia scene Game ở IDEA (`feature-session-scene/session-steps/participant-order`). Ý cốt lõi nêu giá trị IDEA; hai id còn lại vào từ PROTOTYPE. Hộp xác nhận bỏ lượt vẫn là một bước bên trong popup tạm dừng, không có id riêng. Đã loại: giữ 2 id (dịch vụ báo lỗi tải vì thiếu asset của popup IDEA không có).
- **Chuẩn bị sẵn ở bước tải.** Popup thua phải hiện ngay khi thành bị phá; chờ tải giữa trận dễ thấy giật. Đã loại: tải lúc xin mở.
- **Chặn bấm đúp ở feature gọi.** Dịch vụ không biết nút nào là nút; giữ dịch vụ không biết nội dung popup. Đã loại: dịch vụ tự khoá popup sau lần bấm đầu.
- **Rời scene không báo bấm nút.** Tránh feature gọi làm việc (chơi lại, chuyển màn) khi scene đang huỷ.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ id `PopupPause/Main` và `PopupRevive/Main`; mọi giai đoạn chỉ có `PopupLose/Main`; bỏ mục đã tự quyết về hộp xác nhận bỏ lượt.
- **Thêm `PopupRating/Main` (2026-11-27, người quyết, qua @Manager).** Popup mời đánh giá của `feature-popup-rating` chồng lên popup thua khi lượt vừa phá kỉ lục; nó thuộc IDEA nên có trong danh sách từ IDEA.

## Chưa rõ
- Tên `Name` của từng popup do `feature-popup-lose` chốt; đổi tên thì sửa bảng này.
