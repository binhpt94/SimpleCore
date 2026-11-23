---
based-on: b74c0b7d272a
refs: feature-enemy/enemy-stats@504e5ffe01a8, feature-enemy/enemy-reward@1fbbced31e0f, feature-enemy/enemy-behavior@cf9290ce16e6, glossary#melee@b7c5d1d95f25, glossary#ranged@849a367f7047
---
# Danh sách quái

Các loại quái khác nhau ở cách đi và sức chịu, để đợt sau khác đợt trước và mỗi kiểu hero có chỗ mạnh. Có năm loại: Lính, Sói, Khổng lồ, Cung thủ quái và Trùm.

## Ý cốt lõi

### enemy-roster | AI 7 | Năm loại quái: Lính, Sói, Khổng lồ, Cung thủ quái, Trùm
Mỗi loại một kiểu áp lực khác nhau. Đã loại: một loại quái chỉ tăng chỉ số (vòng 2), quái bay (cần thêm làn, trái với chiến trường một làn).

### enemy-soldier | AI 8 | Lính: cận chiến, chỉ số trung bình, ra nhiều nhất
Loại chuẩn để so các loại khác.

### enemy-wolf | AI 7 | Sói: đi nhanh, máu ít, đánh yếu
Ép hero phản ứng nhanh và lọt tới thành nếu đội dồn về bên kia.

### enemy-giant | AI 7 | Khổng lồ: đi chậm, máu nhiều, đánh mạnh
Thử sức đánh của đội; hợp hero đánh một mục tiêu.

### enemy-archer | AI 7 | Cung thủ quái: bắn xa, máu ít, đứng ngoài tầm hero cận chiến
Buộc đội có hero bắn xa hoặc hero cận chiến đi nhanh. Đánh hero và thành từ xa.

### enemy-boss | AI 6 | Trùm: máu và sức đánh cao hơn hẳn mọi loại, đi chậm, ra ít
Tạo đỉnh căng thẳng trong đợt. Khi nào trùm ra là việc của `feature-wave`.
