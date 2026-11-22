---
based-on: 24fd307bbdcd
---
# Thân thành

Thành là một công trình đứng yên giữa dải đất, có máu và không tự hồi. Quái tới thành thì đánh trừ máu thành; máu thành về 0 là thành bị phá và lượt chơi thua. Thành cũng là nơi đội hero xuất phát và hồi sinh.

## Ý cốt lõi

### castle-one | AI 8 | Mỗi trận có đúng một thành, đứng yên giữa dải đất, hai bên là chiến trường
Thành chia dải đất thành bên trái và bên phải. Đã loại: thành di chuyển được, nhiều thành.

### castle-hp | AI 9 | Thành có máu; quái tới thành thì đánh trừ máu thành
Máu là thước đo duy nhất cho biết thành đang an toàn hay sắp nguy. Chỉ quái làm mất máu thành.

### castle-no-regen | AI 8 | Máu thành không tự hồi trong trận
Máu mất ở đợt trước kéo áp lực sang đợt sau; không có lúc nghỉ nào đầy máu lại. Cách duy nhất có thêm máu là nâng cấp máu (`feature-castle/castle-upgrade-effects`). Đã loại: tự hồi chậm theo thời gian, hồi đầy khi dọn sạch đợt.

### castle-destroyed | AI 9 | Máu thành về 0 thì thành bị phá, lượt chơi thua ngay
Thành báo "bị phá" cho `feature-run`; mọi việc sau đó (dừng trận, báo thua) là của `feature-run`.

### castle-hero-base | AI 7 | Thành là điểm xuất phát của đội hero khi vào trận và là nơi hero hồi sinh
Hero sinh ra ngay tại thành rồi tự đi ra hai bên. Thành chỉ là vị trí; luật gục và hồi sinh nằm ở `feature-hero/hero-respawn`. Đã loại: hero hồi sinh tại chỗ gục.
