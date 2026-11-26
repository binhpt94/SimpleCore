# Hero
Tầng: 6 · Features

Đội ba anh hùng trên sân: vào trận theo đội đã chọn, ngắm và đánh quái mỗi tick chiến đấu, ngã rồi hồi lại sau 8 giây, lên cấp khi mua nâng cấp.

## Contracts

### HeroGroup
- Nhóm của anh hùng: `Melee` (cận chiến), `Ranged` (đánh xa). Dùng: TeamSelect, HudUpgrade (qua `HeroInfo`).

### HeroAttack
- Đòn đánh trúng ai: `SingleTarget` chỉ mục tiêu, `FrontCleave` mục tiêu và mọi quái trong khoảng phía trước, `Blast` mục tiêu và mọi quái quanh nó. Dùng: TeamSelect, HudUpgrade.

### HeroInfo
- Một anh hùng trong danh sách, như khi chọn đội. `Code`: khoá ổn định (ví dụ `knight`), đội gồm các code; `Name`: tên hiển thị; `Group`, `Attack`. Dùng: TeamSelect, HudUpgrade.

### IHeroRoster
- Mọi anh hùng chọn được, theo thứ tự hiển thị, cố định suốt game. `Heroes`: danh sách; `TryGet(code, out hero)`: tìm theo code. Dùng: TeamSelect, HudUpgrade.

### IHeroSquad
- Đội trên sân theo ô (0 là ô 1). `HealAll(fraction)`: mọi anh hùng đang đứng hồi `fraction` máu tối đa của mình, không quá tối đa. `ApplyLevel(slot, level)`: anh hùng ở ô lên cấp `level`, có hiệu lực ngay, kể cả khi đang ngã. Dùng: Castle, Upgrade.

### IHeroDownState
- Anh hùng ở ô có đang ngã không. `IsDown(slot)`: true khi đang ngã chờ hồi; false khi đứng hoặc không có lượt. `DownChanged(slot)`: báo trong bước anh hùng ngã hoặc đứng dậy; lượt mới báo cho mọi ô đang ngã. Dùng: HudUpgrade.

### IHeroRespawnTime
- `RespawnSecondsLeft(slot)`: số giây còn lại trước khi anh hùng ở ô quay lại; 0 khi đứng hoặc không có lượt. Chỉ đếm bước trận. Đọc mỗi khung hình. Dùng: chưa feature nào.

### IHeroSpawnPoints
- `SetSpawnX(xBySlot)`: x xuất phát và hồi theo ô, ô 0 trước; thiếu ô thì báo lỗi nêu ô thiếu. Có hiệu lực từ lượt sau. Dùng: Castle.

Test: assembly `Game.Hero.Tests` (EditMode).
