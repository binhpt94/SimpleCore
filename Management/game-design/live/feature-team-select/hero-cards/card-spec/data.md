---
based-on: 5fe7af230b3e
refs: feature-team-select/team-slots/slots-spec@14fc91ea0d47, feature-hero/hero-list@1bf8998ab5a4
---
# Nội dung 5 thẻ hero

Màn chọn đội có 5 thẻ, mỗi thẻ một hero, xếp cố định theo thứ tự danh sách hero. Ghi rõ mỗi thẻ có gì, chữ cách đánh của từng hero, và dấu đã chọn.

## Ý cốt lõi

### cards-five-order | AI 7 | Có 5 thẻ theo thứ tự cố định: Hiệp sĩ, Đấu sĩ rìu, Kiếm khách, Cung thủ, Pháp sư
Cận chiến trước, bắn xa sau. Thứ tự không đổi theo đội đã chọn.

### card-four-parts | AI 7 | Mỗi thẻ có 4 phần: hình hero, tên hero, dòng cách đánh, dấu vai trò
Hình hero là sprite 2D gen bằng AI, cùng hình hero dùng trong trận.

### card-role-text | AI 7 | Dòng cách đánh ghi nhóm và kiểu đòn, ví dụ "Cận chiến · Chém vùng"
Hiệp sĩ "Cận chiến · Chắn", Đấu sĩ rìu "Cận chiến · Chém vùng", Kiếm khách "Cận chiến · Đánh nhanh", Cung thủ "Bắn xa · Tầm xa", Pháp sư "Bắn xa · Nổ vùng".

### card-role-mark-two | AI 6 | Có 2 dấu vai trò: một dấu cho cận chiến (3 thẻ), một dấu cho bắn xa (2 thẻ)
Hai hero cùng nhóm có cùng dấu.

### card-selected-mark | AI 7 | Thẻ của hero đã vào đội có dấu "đã chọn"; bỏ chọn thì dấu mất ngay trong khung hình đó

### card-no-stats | AI 7 | Thẻ không có con số chỉ số nào
Chỉ chữ cách đánh; chỉ số ở `feature-hero` và `feature-balance`.
