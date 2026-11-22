# Đội hero cân nhau

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Danh sách hero và cách đánh của từng hero là của `feature-hero`; node này chỉ quyết chúng phải cân nhau thế nào.
- Danh sách loại quái là của `feature-enemy`; thứ tự mở loại theo đợt là của `feature-wave/difficulty-scaling`.
- Đội được chọn ở `feature-team-select`; nhiều hero hơn số chỗ trong đội.
- Giá nâng cấp hero ở `feature-upgrade`; mỗi hero một mục cấp.

## Định nghĩa
- **Cách đánh**: tổ hợp năm chỉ số chung của một hero (máu, sức đánh, tốc độ đánh, tầm đánh, tốc độ di chuyển) cùng chỗ đứng (chắn trước hay đứng sau).
- **Sức tổng của hero**: giá trị chung của năm chỉ số, dùng để so các hero với nhau ở cùng cấp.
- **Hợp với loại quái**: hero đó hạ loại quái đó nhanh hơn, hoặc chịu được nó lâu hơn so với hero khác.
- **Đội lệch**: đội mà mọi hero cùng một cách đánh.

## Danh sách
Cặp mạnh và yếu đối chiếu cho mỗi hero:
- Máu so với tầm đánh.
- Sức đánh so với tốc độ đánh.
- Tốc độ di chuyển so với máu.

## Luật cơ bản
- Các hero có sức tổng gần bằng nhau ở cùng cấp.
- Mỗi hero có ít nhất một chỉ số mạnh và một chỉ số yếu so với các hero khác.
- Mỗi cách đánh có ít nhất một loại quái nó hợp và một loại nó gặp khó.
- Cùng số tiền nâng cấp thì mỗi hero mạnh lên tương đương nhau.
- Đội trộn cách đánh trụ được nhiều đợt hơn đội lệch; đội lệch vẫn qua được các đợt đầu.

## Đã tự quyết
- **Cân bằng bằng đánh đổi chỉ số, không bằng giá.** Mọi hero chọn được ngay, không mở khoá (vòng 1), nên không thể dùng giá để cân. Đã loại: hero mạnh hơn thì nâng đắt hơn.
- **Đội lệch vẫn chơi được.** Người chơi phổ thông thử đội lạ không bị phạt nặng.

## Chưa rõ
- Danh sách hero cụ thể ở `feature-hero` vòng 3 chưa có; khi có thì đối chiếu lại cặp hero và loại quái.
