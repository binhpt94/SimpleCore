# Nội dung 5 thẻ hero

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Lấy danh sách hero (tên, hình, nhóm, kiểu đòn) từ `feature-hero`; tạo một thẻ cho mỗi hero, theo thứ tự danh sách.
- Mỗi thẻ hiện 4 phần và dấu đã chọn theo trạng thái ở `feature-team-select/team-slots/slots-spec`.
- Chạm thẻ: báo "đã chạm hero X" cho luật chọn; thẻ không tự quyết thêm hay bỏ.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số thẻ | 5 | thẻ | Danh sách 5 hero (`feature-hero/hero-list`) |
| Số phần trên thẻ | 4 + dấu đã chọn | | `card-content` |
| Số loại dấu vai trò | 2 (cận chiến, bắn xa) | | Hai nhóm hero |
| Dòng cách đánh | xem bảng dưới | | Ngắn, đọc được trên điện thoại |

| Hero | Dòng cách đánh | Dấu |
|---|---|---|
| Hiệp sĩ | Cận chiến · Chắn | Cận chiến |
| Đấu sĩ rìu | Cận chiến · Chém vùng | Cận chiến |
| Kiếm khách | Cận chiến · Đánh nhanh | Cận chiến |
| Cung thủ | Bắn xa · Tầm xa | Bắn xa |
| Pháp sư | Bắn xa · Nổ vùng | Bắn xa |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Có màn chọn đội và thẻ | Không (đội mặc định cố định) | Có: hình, tên, dòng cách đánh, dấu đã chọn | Có đủ 4 phần và dấu |
| Dấu vai trò | Không | Có thể chỉ là chữ trong dòng cách đánh | Biểu tượng riêng cho từng nhóm |
| Hiệu ứng khi chọn | Không | Không | Thẻ nảy nhẹ (phóng to rồi về, 0.2 giây, dự kiến) |

## Trường hợp biên
- **Hero mới thêm vào `feature-hero`:** tự có thẻ mới ở cuối nhóm của nó; dòng cách đánh lấy theo nhóm và kiểu đòn.
- **Hình hero chưa có:** thẻ vẫn hiện tên và dòng cách đánh, chỗ hình để trống; không chặn chọn.
- **Chạm thẻ khi đội đã đủ 3 và hero chưa chọn:** thẻ không đổi (luật ở `slots-spec`).

## Đã tự quyết
- **Chữ cách đánh do màn chọn đội ghép từ nhóm và kiểu đòn.** Hiệp sĩ và Kiếm khách cùng "một mục tiêu" nên dùng vai ("Chắn", "Đánh nhanh") để phân biệt. Đã loại: chỉ ghi nhóm (hai hero cận chiến một mục tiêu trông như nhau).
- **Thứ tự cố định, không đổi khi chọn.** Người chơi nhớ chỗ từng hero.

## Chưa rõ
- Chữ vai ("Chắn", "Đánh nhanh", "Tầm xa") cần `feature-hero` đồng ý hoặc cấp sẵn; nếu `feature-hero` có tên vai riêng thì dùng tên đó.
