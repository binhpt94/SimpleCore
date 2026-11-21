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
- View không biết lớp logic: một presenter trong Internal đẩy dữ liệu xuống view qua interface internal (ví dụ `ICoinsDisplay`).
- Không static giữ trạng thái, không singleton, kể cả trong nội bộ feature. View không tự đi tìm logic.
- Feature có view hoặc cần tải thì tham gia `ISessionScene`: tải trong `LoadAsync`, tạo view trong `Run`, huỷ trong `DisposeAsync`. Không đặt sẵn view trong scene.
- Số liệu cân bằng (giá, số xu ban đầu...) nằm trong asset cấu hình ở `FeatureAssets/Private`, không viết cứng trong code.

## Nối giữa các feature
- Lấy dịch vụ của feature khác chỉ qua `IServiceRegistry`, và chỉ trong `Install`. Không static, không singleton dùng chung.
- Installer chỉ nối dây: tạo lớp logic, đăng ký contract. Không chứa luật game. Trong `Install` chỉ tạo object Unity khi đang Play.
- Thiếu phụ thuộc, config hay prefab thì lỗi ngay đầu `Install`, thông báo nêu tên thứ bị thiếu. Không âm thầm bỏ qua.
- Mỗi scene một variant của `Boot.prefab` (`Boot.Loading`, `Boot.Game`) giữ danh sách installer và catalog asset (id → asset) của scene đó; scene không override. Thứ tự cài: SessionScene đầu, bên SceneFlow của scene cuối.
- Cần dùng chung thứ gì với feature khác: đề xuất. Không chép code sang, không đẩy vào `Core`.
- `Core` không chứa luật game và không biết tên feature nào.

## Test
- Mỗi hành vi người chơi thấy được có ít nhất một test EditMode; tên test nói hành vi, không nói tên hàm.
- Feature khác được thay bằng đồ giả viết trong chính file test. Không test bằng Internal thật của feature khác.
- Sửa lỗi thì thêm test tái hiện lỗi trước.
- Mỗi asset installer có test nạp asset thật và gọi `Install` trong EditMode.

## Asset
- Không đổi GUID trong `.meta` đã có. Xoá hay đổi tên asset ở `FeatureAssets/Public` là đổi contract: phải hỏi.
- Asset giao cho dịch vụ (ví dụ prefab popup) là của feature gọi, đặt ở `FeatureAssets/Public` của nó, và đi qua id `<Owner>/<Name>` (ví dụ `Shop/Purchased`), không qua object Unity. Id khai báo một lần, `static readonly` (giá trị bất biến, không phải trạng thái), trong Internal của chủ; validator SRC-05 kiểm.

## Không lách
- Không viết code để né regex của validator (tách chuỗi, gọi gián tiếp API cấm).
- Validator báo sai thì dừng và báo người; sửa `rules.json` chỉ sau khi hỏi và được người đồng ý, từng lần.
