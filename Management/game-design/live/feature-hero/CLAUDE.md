# Hero

Dữ liệu cốt lõi (tóm tắt và các ý có điểm): [data.md](data.md)

## Người chơi được gì
- Lựa chọn trước trận: ghép đội thế nào cho hợp (nhiều cận chiến để chắn, hay nhiều bắn xa để dọn nhanh).
- Xem đội của mình tự đánh và thấy rõ khi chúng mạnh lên.
- Muốn chơi lại để thử đội khác.

## Cốt lõi
- Danh sách hero (dự kiến 6), mỗi hero một cách đánh.
- Màn chọn đội trước lượt chơi, chọn đúng số cố định (dự kiến 4).
- Trong trận: hero tự đánh, gục thì hồi sinh ở thành.
- Mỗi hero có cấp riêng trong lượt, nâng qua `feature-upgrade`.

## Liên quan
- `feature-castle`: hero xuất phát và hồi sinh ở thành.
- `feature-enemy`: hero đánh quái, quái đánh hero.
- `feature-upgrade`: lên cấp từng hero.
- `feature-run`: màn chọn đội nằm giữa màn bắt đầu và trận; hết lượt thì đội và cấp bị xoá.

## Đã tự quyết
- **Màn chọn đội thuộc feature này.** Nó chỉ làm việc với danh sách hero. Đã loại: tách thành feature riêng (quá mỏng).
- **Mỗi hero chọn một lần, không trùng.** Mỗi đội là một tổ hợp khác nhau, có lí do chơi lại. Đã loại: chọn trùng (4 hero giống nhau làm mất lựa chọn).
- **Mở sẵn hết 6 hero.** Không có tiến trình lâu dài (vòng 1, `run-reset`). Đã loại: mở khoá dần.
- **Nâng cấp theo từng hero, một mục tăng mọi chỉ số.** Đội cố định nên nâng từng con giờ có nghĩa; một mục cho mỗi hero giữ ít nút khi đang đánh. Đã loại: nâng theo loại (đã hết ý nghĩa khi mỗi hero là duy nhất), nâng từng chỉ số (quá nhiều nút).
- **Hero gục giữ nguyên cấp khi hồi sinh.** Không phạt nặng việc hero gục. Đã loại: mất cấp khi gục.
- **Hero không có kĩ năng chủ động.** Kĩ năng người chơi bấm nằm ở thành (`feature-castle`). Đã loại: hero có nút kĩ năng.
- **Hero chỉ đuổi quái trong vùng nhìn thấy trên màn hình.**

## Chưa rõ
- Không có điểm chặn.
