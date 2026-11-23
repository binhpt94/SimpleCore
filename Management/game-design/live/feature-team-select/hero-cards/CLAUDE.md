# Thẻ hero

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một hero trong danh sách có đúng một thẻ.
- Mỗi hero có một cách đánh (`feature-hero`); nhiều hero có thể chung một cách đánh.
- Chạm thẻ là chọn hoặc bỏ hero (`feature-team-select/team-slots`).

## Định nghĩa
- `glossary#fighting-style`

## Danh sách
Trên mỗi thẻ:
- Hình hero.
- Tên hero.
- Cách đánh, kèm dấu hiệu vai trò.
- Trạng thái đã chọn hay chưa.

## Luật cơ bản
- Thẻ chỉ hiển thị; không thay đổi gì của hero.
- Hero đã chọn thì thẻ đổi dáng ngay; bỏ chọn thì trở lại như cũ.

## Đã tự quyết
- **Không hiện bảng chỉ số.** Người chơi phổ thông chọn theo vai trò là đủ; chỉ số để `feature-hero` và `feature-balance`. Đã loại: hiện máu, sức đánh, tầm đánh trên thẻ.
- **Cách đánh lấy từ `feature-hero/hero-list` (2026-11-23).** Ý `card-content` trỏ tới node không có (`hero-roles` là một ý của `feature-hero`, không phải node); danh sách hero và vai nằm ở `feature-hero/hero-list`.
- **Không có màn xem chi tiết hero.** Một màn, ít thao tác. Đã loại: chạm giữ để mở thông tin.

## Chưa rõ
- Danh sách vai trò và hero cụ thể chờ vòng 3 của `feature-hero`.
