# Nơi đặt số liệu

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT số liệu thuộc MỘT feature; MỘT feature giữ NHIỀU số liệu trong cấu hình riêng của nó.
- Feature này không giữ số trong code của mình; nó quyết giá trị, feature sở hữu ghi vào cấu hình.
- `feature-balance/balance-difficulty-curve`, `feature-balance/balance-economy`, `feature-balance/balance-team` dùng danh sách này để biết số nào cần đối chiếu.

## Định nghĩa
- **Số liệu**: một giá trị chỉnh được ảnh hưởng tới độ khó, thu chi hay sức của hero, thành, quái.
- **Cấu hình**: nơi một feature giữ số liệu của nó, đổi được mà không sửa code.
- **Feature sở hữu**: feature duy nhất đọc và ghi số liệu đó.

## Danh sách
Số liệu theo feature sở hữu:
- `feature-hero`: năm chỉ số chung của từng hero, thời gian hồi sinh, sức thêm mỗi cấp.
- `feature-castle`: máu, sức đánh, tầm đánh, tốc độ đánh của thành; sức và thời gian hồi của từng kĩ năng; sức thêm mỗi cấp của ba mục.
- `feature-enemy`: năm chỉ số chung và tiền gốc của từng loại quái.
- `feature-wave`: số quái, nhịp thả, hệ số máu, sức đánh, tiền theo đợt; đợt mở từng loại quái; thưởng dọn sạch; độ dài lúc thở.
- `feature-upgrade`: giá ban đầu và mức tăng giá theo cấp của từng mục.
- `feature-wallet`: tiền lúc bắt đầu lượt.

## Luật cơ bản
- Không có số liệu nằm ở hai feature cùng lúc.
- Đổi một số liệu thì xem lại các số có quan hệ trong danh sách trên.
- Mọi số liệu trong danh sách đều đổi được qua cấu hình.

## Đã tự quyết
- **Không có bảng số dùng chung.** Theo luật project: không tạo phần dùng chung giữa các feature. Đã loại: một bảng số trung tâm.
- **Không có công cụ mô phỏng.** Ngoài ý tưởng vòng 1; bảng đối chiếu và chơi thử là đủ. Đã loại: mô phỏng tự động.
- **Trả lời mục "Chưa rõ" của `feature-balance`** (có công cụ xem chung các số hay không): không có công cụ trong game, chỉ có danh sách đối chiếu ở node này.

## Chưa rõ
- `feature-balance` có cần thư mục code riêng hay không: hướng hiện tại là không, vì mọi số nằm ở cấu hình của feature khác.
