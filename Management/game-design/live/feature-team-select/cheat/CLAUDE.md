# Cheat: màn chọn đội

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `full-team` thì game hiện ngay màn chọn đội với đội nhớ ép là Hiệp sĩ, Đấu sĩ rìu, Cung thủ.
- Đội ép đi qua đúng đường của game: đổ vào 3 chỗ như đội nhớ thật (`feature-team-select/team-memory/memory-rules`), thẻ và nút tự đổi trạng thái theo luật (`feature-team-select/team-slots/slots-spec`, `feature-team-select/hero-cards/card-spec`).
- Code cheat nằm trong thư mục `TeamSelect`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Một state: đội đủ 3, nút sáng.** Có cả thẻ đã chọn lẫn chưa chọn, cả 3 chỗ có hero, nút bấm được. Đội thiếu (chỗ trống, nút mờ) chỉ ẩn bớt hero khỏi chỗ và làm mờ nút, không thêm thành phần, nên không tách state. Đã loại: state "đội trống".
- **Frame `use` toàn bộ số** từ `slots-spec` (tiêu đề, lưới thẻ, lưới chỗ, nút) và `card-spec` (các phần trong thẻ), cùng `feature-hud/hud-layout/layout-spec` (cao nút). Các phần trong thẻ Hiệp sĩ (hình, tên, cách đánh, dấu, dấu đã chọn) đặt thẳng trong ô đầu của lưới thẻ (`parent=cards`; ô đầu trùng góc lưới) để thấy các phần trong thẻ; 4 thẻ còn lại ghi trong nhãn của lưới. Để `use` được, `slots-spec/main.frame` và `card-spec/card.frame` đã chuyển các số sang `let` (cùng giá trị, đổi hash hai node đó); `card-spec` lấy vị trí, cỡ thẻ từ `slots-spec`.
- **Bỏ hình khung `card1` riêng (2026-11-24).** Claude-concept dựng cheat từ phần cùng id hoặc cùng loại, cùng cỡ; `card1` (round-rect) không khớp phần nào nên chỉ là hình cơ bản đè lên ô thẻ đã vẽ từ `slots-spec` (phần `cards`). Các phần con gắn vào `cards` thì ảnh dùng đúng khung thẻ đã vẽ. Đã loại: giữ `card1` (che mất khung thẻ trong ảnh); đổi `card-spec` để tự vẽ khung (khung thẻ đã thuộc `slots-spec`).
- **Ảnh art-concept của màn chọn đội chỉ đặt ở node này**, một ảnh cho state `full-team`, style `ui-flat`.

## Chưa rõ
- Không có điểm chặn.
