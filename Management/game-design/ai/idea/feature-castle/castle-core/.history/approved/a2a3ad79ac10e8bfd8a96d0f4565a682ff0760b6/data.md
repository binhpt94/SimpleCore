---
based-on: 7100f997d2a8
refs: feature-castle/castle-upgrade-effects@2c1e443d0ced, feature-hero/hero-respawn@2ae1008b33cd, feature-enemy/enemy-behavior@cf9290ce16e6, glossary#reach-castle@7db7d78f37e0
---
# Thân thành

Thành là một công trình đứng yên giữa dải đất, có máu và không tự hồi. Quái tới thành thì đánh trừ máu thành; máu thành về 0 là thành bị phá. Lần đầu bị phá trong lượt có thể được hồi sinh bằng quảng cáo; không thì lượt thua. Thành cũng là nơi đội hero xuất phát và hồi sinh.

## Ý cốt lõi

### castle-one | AI 8 | Mỗi trận có đúng một thành, đứng yên giữa dải đất, hai bên là chiến trường
Thành chia dải đất thành bên trái và bên phải. Đã loại: thành di chuyển được, nhiều thành.

### castle-hp | AI 9 | Thành có máu; quái tới thành thì đánh trừ máu thành
Máu là thước đo duy nhất cho biết thành đang an toàn hay sắp nguy. Chỉ quái làm mất máu thành.

### castle-no-regen | AI 8 | Máu thành không tự hồi trong trận
Máu mất ở đợt trước kéo áp lực sang đợt sau; không có lúc nghỉ nào đầy máu lại. Chỉ hai cách có thêm máu: nâng cấp máu (`feature-castle/castle-upgrade-effects`) và hồi sinh thành. Đã loại: tự hồi chậm theo thời gian, hồi đầy khi dọn sạch đợt.

### castle-destroyed | AI 9 | Máu thành về 0 thì thành bị phá và báo `feature-run` đúng một lần
Mọi việc sau đó (dừng trận, đề nghị hồi sinh hay báo thua) là của `feature-run`.

### castle-revive-restore | AI 7 | Được hồi sinh thì thành đứng lại với một phần máu tối đa; cấp nâng cấp và máu tối đa giữ nguyên
Hồi sinh do `feature-run` ra lệnh sau khi người chơi xem xong quảng cáo; thành chỉ đổi máu hiện tại. Bị phá lần nữa thì báo lại như lần đầu.

### castle-hero-base | AI 7 | Thành là điểm xuất phát của đội hero khi vào trận và là nơi hero hồi sinh
Hero sinh ra ngay tại thành rồi tự đi ra hai bên. Thành chỉ là vị trí; luật gục và hồi sinh nằm ở `feature-hero/hero-respawn`. Đã loại: hero hồi sinh tại chỗ gục.
