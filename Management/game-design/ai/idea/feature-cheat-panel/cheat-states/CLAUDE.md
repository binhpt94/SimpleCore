# State trong panel cheat
Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một panel, nhiều dòng; mỗi dòng là một state của đúng một feature.
- Một feature có thể có nhiều dòng (node cheat của nó có tối đa 3 state).
- State chỉ ở bản dev mà bản build đang chạy không có thì không có dòng.

## Định nghĩa
- Không có từ mới cần đưa vào glossary.

## Danh sách
- Các dòng state, lấy từ node cheat của từng feature.
- Một dòng chơi bình thường.

## Luật cơ bản
- Chạm dòng state: feature sở hữu dựng state đó, panel đóng.
- Chạm dòng chơi bình thường: bỏ state cheat, game về như lúc không cheat, panel đóng.

## Chưa rõ
- Không còn điểm nào chưa rõ ở vòng này.

## Đã tự quyết
- Feature sở hữu dựng state: không lặp giá trị ép ở hai chỗ. Đã loại: panel tự ép giá trị qua các giao diện cheat lẻ.
- Danh sách lấy theo node cheat: thêm state không phải sửa panel.
