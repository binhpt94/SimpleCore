# Quái đi và đánh

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT quái thuộc MỘT bên (trái hoặc phải), theo mép nó ra (`feature-wave`).
- MỘT quái đánh MỘT mục tiêu mỗi đòn: một hero hoặc thành.
- NHIỀU quái có thể cùng đánh MỘT hero hoặc thành.
- Quái chỉ gặp hero đang đứng ở bên của nó.

## Định nghĩa
- `glossary#side` (bên của quái là bên có mép nó ra)
- `glossary#reach-castle`

## Danh sách
Trạng thái của quái:
- Đi: không có hero trong tầm, chưa tới thành; đi về phía thành theo tốc độ di chuyển.
- Đánh hero: có hero trong tầm; đứng yên, đánh hero gần nhất.
- Đánh thành: đã tới thành, không có hero trong tầm; đứng yên, đánh thành.
- Bị hạ (`feature-enemy/enemy-stats`).

## Luật cơ bản
- Quái ra ở mép bên mình rồi đi về phía thành.
- Hero trong tầm: hero còn trên chiến trường và nằm trong tầm đánh của quái.
- Có hero trong tầm: dừng lại, đánh hero gần nhất cho tới khi hero đó gục hoặc ra khỏi tầm.
- Hero trong tầm và thành trong tầm cùng lúc: đánh hero.
- Không còn hero trong tầm: đi tiếp, hoặc đánh thành nếu đã tới thành.
- Quái không đuổi theo hero, không quay đầu, không đi qua thành.
- Quái không chắn nhau, không đánh nhau.
- Bị Băng giá làm chậm: đi chậm hơn và đánh chậm hơn trong lúc bị chậm (`feature-castle/castle-skills`).
- Thưởng-phạt: quái lọt tới thành là thành mất máu (`feature-castle/castle-core`); hero chắn được quái thì thành an toàn.

## Đã tự quyết
- **Ưu tiên đánh hero hơn thành.** Giữ vai chắn của hero cận chiến; người chơi thấy rõ hero đang bảo vệ thành. Đã loại: ưu tiên thành.
- **Quái không đuổi hero.** Hero tự đi tới quái (`feature-hero/hero-targeting`); cho quái đuổi thì hai bên đuổi nhau, khó đọc.
- **Quái không chắn nhau.** Một làn mà quái chắn nhau thì quái sau kẹt vô hạn sau con trước; đi qua nhau giữ luật đơn giản và vẫn dồn cụm khi hero chắn.
- **Quái không sang bên kia thành.** Thành ở giữa là đích của cả hai bên.

## Chưa rõ
- Không có điểm chặn.
