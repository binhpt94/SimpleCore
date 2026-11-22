# Chọn đội

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Một quyết định đáng nghĩ trước trận: ghép đội nhiều chắn hay nhiều bắn xa.
- Chơi lại nhanh: đội cũ đã chọn sẵn.

## Liên quan
- `feature-hero`: danh sách hero và cách đánh của từng hero.
- `feature-run`: màn này nằm giữa màn bắt đầu và trận; đội vừa chọn vào trận.

## Đã tự quyết
- **Màn chọn đội là feature riêng, tách khỏi `feature-hero`.** Là một màn hình riêng trong luồng lượt chơi, giao được cho một phiên. Đã loại: để chung trong `feature-hero`.
- **Nhớ đội lượt trước.** Lấy từ quyết định "chơi lại về màn chọn đội" của `feature-run`.

## Chưa rõ
- Mỗi hero chỉ chọn một lần hay chọn trùng được: vòng 3 (hướng hiện tại: không trùng).
