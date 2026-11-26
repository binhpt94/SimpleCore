# Đọc, ghi và khôi phục dữ liệu lưu

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một bản ghi cho cả game. Một bên gọi: `feature-run`.
- `feature-run` gọi đọc một lần lúc mở game, gọi ghi mỗi khi kỉ lục đổi hoặc khi thử ghi lại.
- Lưu dữ liệu không biết kỉ lục nghĩa là gì: nó cất một số nguyên và trả lại đúng số đó.

## Định nghĩa
- **Bản ghi lưu:** thứ nằm trên máy, gồm số phiên bản dữ liệu và kỉ lục.
- **Phiên bản dữ liệu:** số cho biết bản ghi được viết theo dạng nào. Đổi dạng bản ghi thì tăng số này.
- **Chưa có:** kết quả đọc khi không lấy được kỉ lục dùng được.

## Danh sách
Kết quả đọc: có kỉ lục · chưa có.
Kết quả ghi: thành công · thất bại.
Trường hợp bản ghi lúc đọc: không có · đúng phiên bản hiện tại · phiên bản cũ hơn · phiên bản mới hơn · hỏng.

## Luật cơ bản
- Đọc: đúng phiên bản hoặc cũ hơn → có kỉ lục; không có, mới hơn, hỏng → chưa có.
- Ghi: phiên bản trên máy mới hơn → thất bại, không đụng bản ghi; còn lại → thay cả bản ghi.
- Ghi dở không bao giờ để lại bản ghi nửa cũ nửa mới.

## Đã tự quyết
- **Đọc chỉ có hai kết quả**, theo `save-missing-empty` của node cha (đã loại ý báo riêng từng lí do).
- **Đọc một lần lúc mở game.** Lượt chơi giữ kỉ lục trong bộ nhớ; đọc lại mỗi lần hiện màn sẽ ghi đè kỉ lục chưa lưu được khi ghi lỗi (`run-keep-on-fail`).
- **Hỏng thì không tự xoá.** Theo `save-keep-unreadable`.

## Chưa rõ
- Không có điểm chặn.
