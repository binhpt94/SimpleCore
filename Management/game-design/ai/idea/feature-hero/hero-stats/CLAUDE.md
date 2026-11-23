# Chỉ số và cấp hero

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT hero có MỘT bộ năm chỉ số và MỘT cấp.
- MỘT lần mua "cấp hero" tăng MỘT cấp cho MỘT hero (không tăng cho cả đội).
- Mỗi hero trong đội có cấp riêng. Theo hướng hiện tại của `feature-team-select` (không chọn trùng hero), một loại hero chỉ có một cấp trong đội.
- Bộ chỉ số này cũng dùng cho quái (`feature-enemy/enemy-stats`) và thành (`feature-castle/castle-attack`, `feature-castle/castle-core`).

## Định nghĩa
- `glossary#range`
- `glossary#level`

## Danh sách
| Chỉ số | Hero | Quái | Thành | Cấp hero tăng |
|---|---|---|---|---|
| Máu | có | có | có | có |
| Sức đánh | có | có | có | có |
| Tốc độ đánh | có | có | có | có |
| Tầm đánh | có | có | có | không |
| Tốc độ di chuyển | có | có | không | không |

## Luật cơ bản
- Một đòn trúng: máu mục tiêu giảm đúng bằng sức đánh của bên đánh.
- Lên cấp: máu tối đa, sức đánh, tốc độ đánh của hero tăng; tầm đánh và tốc độ di chuyển giữ nguyên.
- Lên cấp khi đang đánh: máu hiện tại tăng thêm đúng phần máu tối đa vừa tăng, giống cách nâng máu thành (`feature-castle/castle-upgrade-effects`).
- Mua cấp có hiệu lực ngay, kể cả khi hero đang gục (`feature-upgrade`).
- Cấp giữ tới hết lượt.
- Thưởng-phạt: không mất gì khi gục; tiền vào hero này là tiền không vào hero khác hay vào thành.

## Đã tự quyết
- **Không có giáp, chí mạng, né.** Năm chỉ số là đủ để các cách đánh khác nhau; người chơi phổ thông đọc được. Đã loại: giáp trừ sát thương.
- **Cấp hero tăng máu, sức đánh, tốc độ đánh; không tăng tầm và tốc độ di chuyển.** Tầm và tốc độ đi là thứ làm nên vai của hero (đứng trước hay đứng sau); tăng chúng làm hero bắn xa lấn vai cận chiến. Quyết định chung "tăng mọi chỉ số" hiểu là mọi chỉ số chiến đấu (sức chịu và sức gây sát thương). Đã loại: tăng cả năm.
- **Lên cấp cộng phần máu tăng vào máu hiện tại.** Mua xong thấy ngay; giống luật của thành nên một luật cho cả game. Đã loại: hồi đầy máu khi lên cấp.
- **Định nghĩa chỉ số chung chuyển sang `glossary` (2026-11-23).** Trước đặt ở node này; giờ hero, quái, thành cùng trỏ về glossary.

## Chưa rõ
- Không có điểm chặn. Lưu ý cho người: "cấp hero tăng mọi chỉ số" ở quyết định chung được hiểu là không tăng tầm và tốc độ di chuyển; nếu người muốn tăng cả năm thì chấm lại ý `hero-level`.
