# Cheat: màn Loading

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `load-error` thì màn Loading dừng ở 40% và hiện dòng lỗi "Lỗi tải: Wallet", như khi feature Wallet tải lỗi thật (`feature-scene-flow/scene-list/boot-loading`).
- Cheat chỉ ép giá trị hiển thị (tiến độ, tên feature lỗi); không làm hỏng asset thật.
- Code cheat nằm trong thư mục `SceneFlow`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Một state: tải lỗi.** State này có đủ mọi thành phần (tên game, thanh tiến độ, dòng lỗi); màn Loading bình thường chỉ ẩn bớt dòng lỗi nên không tách state theo luật node cheat. Đã loại: state "đang tải 40%".
- **Feature lỗi mẫu là Wallet.** Tên ngắn, đọc rõ trong ảnh; chỉ là giá trị mẫu.
- **Frame `use` toàn bộ số** từ `boot-loading`. Để `use` được, `boot-loading/main.frame` đã chuyển các số sang `let` (cùng giá trị, đổi hash node đó). Mức thanh 0.4 là giá trị ép của state, không phải số bố cục.
- **Ảnh art-concept của màn Loading chỉ đặt ở node này**, một ảnh cho state `load-error`, style `ui-flat`.

## Chưa rõ
- Không có điểm chặn.
