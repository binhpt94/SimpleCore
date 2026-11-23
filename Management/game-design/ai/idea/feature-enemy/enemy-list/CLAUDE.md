# Danh sách quái

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT đợt có NHIỀU quái, có thể thuộc NHIỀU loại (`feature-wave`).
- MỘT quái thuộc MỘT loại, không đổi loại.
- Mọi loại quái đánh MỘT mục tiêu mỗi đòn.
- Loại nào ra ở đợt nào, bên nào: `feature-wave`. Giá trị chỉ số: `feature-balance`.

## Định nghĩa
- `glossary#melee`
- `glossary#ranged`

## Danh sách
| Loại | Đánh | Máu | Tốc độ đi | Sức đánh | Hero hợp để đối phó |
|---|---|---|---|---|---|
| Lính | Cận chiến | Trung bình | Trung bình | Trung bình | Mọi hero |
| Sói | Cận chiến | Ít | Nhanh nhất | Yếu | Đòn vùng, hero đánh nhanh |
| Khổng lồ | Cận chiến | Nhiều | Chậm | Mạnh | Hero một mục tiêu sức đánh cao |
| Cung thủ quái | Bắn xa | Ít | Trung bình | Trung bình | Hero bắn xa, Kiếm khách |
| Trùm | Cận chiến | Nhiều nhất | Chậm nhất | Mạnh nhất | Cả đội, kĩ năng thành |

So sánh tiền thưởng: Trùm nhiều nhất, Khổng lồ nhiều hơn Lính, Sói và Cung thủ quái ngang hoặc ít hơn Lính (`feature-enemy/enemy-reward`).

## Luật cơ bản
- Mọi loại theo cùng một luật đi và đánh (`feature-enemy/enemy-behavior`); khác nhau chỉ ở chỉ số và tiền thưởng.
- Trùm là quái thường với chỉ số rất cao, không có đòn riêng.
- Loại trâu hơn cho nhiều tiền hơn.

## Đã tự quyết
- **Năm loại, bốn loại thường và một trùm.** Mỗi loại thường đè một kiểu hero khác nhau, nên đội lệch một phía sẽ thấy khó; trùm cho đỉnh căng thẳng. Đã loại: quái bay (cần làn thứ hai), quái phá thành chuyên đánh thành (trùng vai Khổng lồ), quái nổ tự sát (thêm luật riêng).
- **Có một loại quái bắn xa.** Cho hero bắn xa việc riêng ngoài việc đứng sau. Đã loại: mọi quái cận chiến.
- **Trùm không có đòn đặc biệt.** Giữ một luật hành vi cho mọi quái. Đã loại: trùm có kĩ năng.
- **Tên quái là tên tạm theo vai.** Đổi tên không đổi luật.

## Chưa rõ
- Không có điểm chặn. Đợt nào có trùm: `feature-wave` quyết.
