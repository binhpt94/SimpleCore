---
based-on: b9aace0d5890
refs: feature-cheat-panel/cheat-states@1a6897ef3a27, feature-cheat-panel/cheat-states/states-rules@92e35b523d22
---
# Config, nút và panel cheat

Config `cheat_enabled` bật thì có nút cheat ở mọi màn; chạm nút mở panel cheat chứa danh sách state; panel chặn chạm xuống game và đóng bằng chính nút đó.

## Ý cốt lõi

### spec-config-asset | AI 8 | Công tắc là một ô bật tắt `cheat_enabled` trong asset config của feature, đi kèm bản build
Người làm game đổi ô này trước khi build; bản build mang giá trị lúc build. Đã loại: đọc từ file ngoài trên máy (thêm đường bật cheat cho người chơi thật).

### spec-default-on | AI 8 | `cheat_enabled` mặc định là bật
Bản build bây giờ đều là bản test. Tắt là việc của người làm game khi cần một bản sạch.

### spec-any-build | AI 7 | Chỉ `cheat_enabled` quyết có nút hay không, ở cả Editor và mọi loại bản build
Loại build không tự ẩn nút; state nào có trong panel thì theo `feature-cheat-panel/cheat-states`.

### spec-off-nothing | AI 8 | `cheat_enabled` tắt thì không tạo nút, không tạo panel, không chặn chạm nào
Game y như không có feature này.

### spec-button-label | AI 7 | Nút cheat mang chữ "Cheat"
Chữ thay cho hình: art IDEA siêu cơ bản, người test đọc là hiểu.

### spec-button-state | AI 7 | Nút cheat có hai trạng thái nhìn khác nhau: panel đóng và panel đang mở

### spec-panel-parts | AI 8 | Panel gồm tiêu đề "Cheat", danh sách dòng cuộn được và nền che chặn chạm phía dưới
Nền che phủ cả màn hình dưới panel, trên game và popup.

### spec-close-ways | AI 7 | Panel đóng khi chạm lại nút cheat, khi chọn một dòng, hoặc khi game đổi scene
Chạm vào nền che không đóng panel, để chạm nhầm không làm mất chỗ đang cuộn.

### spec-transition-lock | AI 7 | Trong lúc game đang chuyển scene, các dòng của panel bị khoá
Tránh chọn state chồng lên một lần chuyển scene đang chạy; nút cheat vẫn đóng mở panel được.
