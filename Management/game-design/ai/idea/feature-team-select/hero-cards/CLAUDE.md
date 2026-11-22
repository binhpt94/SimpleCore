# Thẻ hero

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một hero trong danh sách có đúng một thẻ.
- Mỗi hero có một cách đánh (`feature-hero/hero-roles`); nhiều hero có thể chung một cách đánh.
- Chạm thẻ là chọn hoặc bỏ hero (`feature-team-select/team-slots`).

## Định nghĩa
- **Thẻ hero**: ô hiện một hero trong danh sách.
- **Cách đánh**: vai trò của hero trong trận, như chắn quái ở trước hay bắn xa ở sau.
- **Dấu hiệu vai trò**: biểu tượng hoặc màu chung cho các hero cùng cách đánh.

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
- **Không có màn xem chi tiết hero.** Một màn, ít thao tác. Đã loại: chạm giữ để mở thông tin.

## Chưa rõ
- Danh sách vai trò và hero cụ thể chờ vòng 3 của `feature-hero`.
