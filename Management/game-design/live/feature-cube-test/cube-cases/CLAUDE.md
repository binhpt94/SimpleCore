# Các khối thử và trường hợp hiển thị

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Ba khối, mỗi khối đúng một chỗ trong thứ tự vẽ của `feature-battlefield/battlefield-layers`: sau thành, giữa thành và hero, quái, trước hero, quái.
- Khối không biết gì về thành, hero, quái: chỉ đứng ở chỗ cố định và chen vào thứ tự vẽ. Quái đi từ mép màn vào thành nên tự đi ngang qua khối.
- HUD và popup luôn trên khối.

## Định nghĩa
- Không có từ mới.

## Danh sách
- Khối sau thành.
- Khối giữa thành và hero, quái.
- Khối trước hero, quái.

## Luật cơ bản
- Cả ba khối cùng hình, cùng cỡ, cùng màu, cùng cách sáng, chỉ khác chỗ đứng và chỗ trong thứ tự vẽ: khác nhau chỉ ở thứ cần so.
- Khối không chặn ai, chạm vào không làm gì (theo ý `cube-no-effect` của node cha).

## Đã tự quyết
- **Ba khối cùng lúc** thay vì một khối đổi chỗ: không cần node cheat hay nút bật để xem đủ trường hợp. Đã loại: một khối và cheat đổi thứ tự.
- **Bóng là hình phẳng**, không bóng thật theo đèn: sprite 2D không đổ bóng thật, so được với bóng của hero và quái.
- **Xoay chậm** điểm 6: giúp xem sáng tối, nhưng sprite của trận không xoay; nếu người thấy rối thì bỏ.

## Chưa rõ
- Không có điểm chặn.
