# Cheat: màn bắt đầu

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `with-record` thì game hiện ngay màn bắt đầu với kỉ lục ép là 15.
- Kỉ lục ép đi qua đúng đường của game: màn đọc kỉ lục từ `feature-run/run-score/score-rules` như bình thường (`feature-home/home-screen/home-screen-spec`).
- Code cheat nằm trong thư mục `Home`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Một state: có kỉ lục.** Kỉ lục 0 chỉ là cùng thành phần với số khác, không thêm phần nào, nên không tách state. Đã loại: thêm state "kỉ lục 0".
- **Frame `use` toàn bộ số** từ `home-screen-spec` (vị trí tên game, kỉ lục, nút) và `feature-hud/hud-layout/layout-spec` (cao nút). Để `use` được, `home-screen-spec/main.frame` đã chuyển các số sang `let` (cùng giá trị, đổi hash node đó).
- **Ảnh art-concept của màn bắt đầu chỉ đặt ở node này**, một ảnh cho state `with-record`, style `ui-flat`.

## Chưa rõ
- Không có điểm chặn.
