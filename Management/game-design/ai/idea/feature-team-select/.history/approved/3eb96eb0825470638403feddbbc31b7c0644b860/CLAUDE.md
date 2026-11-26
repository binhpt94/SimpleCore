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

- **Đưa góp ý GPT lần hỏi `r2-1` (2026-11-24).** `team-compare` viết lại theo `hien-vai-tro-hero` (AI 7): nói vai trò phòng thủ thay cho "cách đánh", sát mục đích chọn đội.
- **Loại ý `team-default` (góp ý `chon-san-doi-mac-dinh`, thay `team-remember`), AI 4.** Góp ý dựa trên đọc `run-reset` ở gốc thành "không giữ đội", trong khi gốc ghi người chơi giữ đội cũ để thử lại và `run-replay` của `feature-run` về màn chọn đội với đội cũ. Thay nhớ đội bằng đội mặc định sẽ làm mất đường chơi lại nhanh với cùng đội. Đội mặc định vẫn có cho lần đầu (nhánh nhớ đội đã chốt). Khai `gpt-dropped`.

## Chưa rõ
- Mỗi hero chỉ chọn một lần hay chọn trùng được: vòng 3 (hướng hiện tại: không trùng).
