# Luật Unity mà validator không kiểm được
Validator ép ranh giới. File này ghi phần còn lại; người duyệt sẽ soi theo từng mục.

## Contract
- Chưa có feature khác cần thì để trong Internal. Không mở contract "phòng khi cần".
- Contract chỉ gồm interface, enum, struct dữ liệu chỉ đọc. Không có logic, không có trạng thái sửa được.
- Tên và tham số nói bằng từ của feature mình, không nhắc tới feature gọi nó.
- Contract đã có feature khác dùng: không đổi nghĩa, không đổi chữ ký. Cần khác thì đề xuất.

## Internal và View
- Luật game nằm trong lớp C# thuần ở Internal, chạy được trong test không cần scene.
- Việc phải đụng Unity (tạo, huỷ object, thời gian, lưu file) đặt sau một interface trong Internal để test thay bằng đồ giả.
- View chỉ hiển thị và chuyển thao tác người chơi xuống Internal. Không tính toán, không quyết định kết quả.
- Số liệu cân bằng (giá, số xu ban đầu...) nằm trong asset cấu hình ở `FeatureAssets/Private`, không viết cứng trong code.

## Nối giữa các feature
- Lấy dịch vụ của feature khác chỉ qua `IServiceRegistry`, và chỉ trong `Install`. Không static, không singleton dùng chung.
- Installer chỉ nối dây: tạo lớp logic, đăng ký contract. Không chứa luật game.
- Thiếu phụ thuộc thì lỗi ngay lúc `Install`, thông báo nêu tên contract bị thiếu. Không âm thầm bỏ qua.
- Cần dùng chung thứ gì với feature khác: đề xuất. Không chép code sang, không đẩy vào `Core`.
- `Core` không chứa luật game và không biết tên feature nào.

## Test
- Mỗi hành vi người chơi thấy được có ít nhất một test EditMode; tên test nói hành vi, không nói tên hàm.
- Feature khác được thay bằng đồ giả viết trong chính file test. Không test bằng Internal thật của feature khác.
- Sửa lỗi thì thêm test tái hiện lỗi trước.

## Asset
- Không đổi GUID trong `.meta` đã có. Xoá hay đổi tên asset ở `FeatureAssets/Public` là đổi contract: phải hỏi.
- Prefab giao cho dịch vụ (ví dụ Popup) là của feature gọi, đặt trong feature gọi.

## Không lách
- Không viết code để né regex của validator (tách chuỗi, gọi gián tiếp API cấm).
- Validator báo sai thì dừng và báo người; không tự thêm ngoại lệ vào `rules.json`.
