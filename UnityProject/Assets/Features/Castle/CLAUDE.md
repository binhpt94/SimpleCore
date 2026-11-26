# Castle
Tầng: 6 · Features

Thành của người chơi: máu, lúc bị phá (báo thua cho lượt), các skill bấm tay có hồi chiêu, và nhận cấp nâng trong lượt.

## Contracts
Feature dùng từng contract: chưa rà hết (guard không cho tìm ngoài vùng Castle). @CodeLead bổ sung khi nối dây.

### ICastle
- Thân thành như bên ngoài thấy. Quái đánh thành qua `Game.Enemy.IEnemyFoes`, do Castle tự thêm vào sân quái.
- `Health`, `MaxHealth`: máu hiện tại và máu tối đa.
- `IsDestroyed`: true từ lúc máu về 0.
- `HealthChanged`: báo sau mỗi lần `Health` hoặc `MaxHealth` đổi.
- `Destroyed`: báo một lần khi máu về 0, sau khi đã báo lượt thua.

### CastleSkill
- Enum các skill của thành: `Thunder` (gây sát thương mọi quái trong vùng quanh thành), `Frost` (làm chậm mọi quái còn sống), `Rally` (hồi máu mọi anh hùng còn đứng).

### CastleSkillState
- Struct chỉ đọc: trạng thái một skill tại lúc đọc.
- `Skill`: skill nào. `Ready`: true khi bấm lúc này thì dùng được. `CooldownLeft`: số giây tới khi sẵn sàng, 0 khi sẵn sàng. `Cooldown`: hồi chiêu đủ, tính bằng giây.

### ICastleSkills
- Skill người chơi bấm. Thành giữ mọi hồi chiêu; bên gọi chỉ hiển thị và yêu cầu.
- `Skills`: các skill của trận này, theo thứ tự nút.
- `GetState(skill)`: trả `CastleSkillState` hiện tại.
- `TryUse(skill)`: dùng skill khi nó sẵn sàng và thành còn đứng; bắt đầu hồi chiêu, trả true. Ngoài ra trả false, không đổi gì.

### CastleUpgrade
- Enum phần thành nâng được trong lượt: `Health`, `Attack`, `Skills`.

### ICastleUpgrades
- Nhận cấp đã mua cho thành; giá và xu không phải việc của Castle.
- `ApplyLevel(item, level)`: `item` giờ ở cấp `level`; thành áp hiệu ứng ngay.

### ICastleCheat
- Chỉ bản dev (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`): đưa thành vào state cheat mà không phải chơi tới. Mọi thay đổi báo qua `HealthChanged` và trạng thái skill như thường.
- `SetHealth(health)`: đặt máu, kẹp trong 1..MaxHealth (cheat không phá thành); không làm gì khi thành đã bị phá.
- `SetCooldownLeft(skill, secondsLeft)`: đặt hồi chiêu còn lại, kẹp trong 0..hồi chiêu đủ; 0 là sẵn sàng. Không dùng skill (không có hiệu ứng).

### ICastleFallCheat
- Chỉ bản dev: phá thành có chủ đích, cho các state cheat bắt đầu ngay sau khi thành sập.
- `Fall()`: đưa máu về 0 theo đường sập thường (`feature-castle/castle-core/castle-body-values`), báo lượt đúng một lần như sập thật, lượt thua. Không làm gì khi thành đã bị phá hoặc không có trận đang chạy.

Test: assembly `Game.Castle.Tests` (EditMode).
