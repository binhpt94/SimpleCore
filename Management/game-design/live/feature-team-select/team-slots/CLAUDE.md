# Chỗ trong đội và luật chọn

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Danh sách hero (toàn bộ hero của game) lấy từ `feature-hero`; mọi hero chọn được ngay (`feature-hero`).
- Một đội có nhiều chỗ; mỗi chỗ có một hero hoặc để trống.
- Một hero ở nhiều nhất một chỗ.
- Bấm vào trận thì đưa đội cho `feature-run/run-start`; chuyển màn do `feature-run/run-flow`.
- Đội lúc mở màn lấy từ `feature-team-select/team-memory`.

## Định nghĩa
- **Danh sách hero**: mọi hero có trong game.
- **Chỗ**: một ô trong đội, chứa được một hero.
- **Đội đủ**: mọi chỗ đều có hero.
- **Hero đã chọn**: hero đang nằm ở một chỗ trong đội; trong danh sách được đánh dấu là đã chọn.

## Danh sách
Thứ trên màn chọn đội:
- Danh sách hero (mỗi hero một thẻ, nội dung thẻ ở `feature-team-select/hero-cards`).
- Các chỗ trong đội.
- Nút vào trận.

## Luật cơ bản
- Chạm hero chưa chọn: hero vào chỗ trống đầu tiên.
- Chạm hero đã chọn (ở danh sách hoặc ở chỗ trong đội): hero rời đội, chỗ đó trống.
- Đội đã đủ thì chạm hero chưa chọn không làm gì; phải bỏ một hero ra trước.
- Đội đủ thì nút vào trận bấm được; chưa đủ thì nút mờ.

## Đã tự quyết
- **Không trùng hero.** Theo quyết định chung; trả lời "Chưa rõ" của `feature-team-select`.
- **Đội đủ thì chạm hero mới không tự đổi chỗ.** Tránh vô tình đẩy hero ra; người chơi tự bỏ trước. Đã loại: tự thay hero ở chỗ cuối.
- **Thứ tự trong đội không có nghĩa.** Hero tự đánh theo cách đánh, không theo chỗ. Đã loại: chỗ quyết định vị trí đứng trên chiến trường.

## Chưa rõ
- Số chỗ và số hero cụ thể: vòng 4, cùng `feature-balance` và `feature-hero`.
