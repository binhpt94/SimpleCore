# Combat
Tầng: 6 · Features

Phân xử mọi đòn trúng trong một nhịp giao chiến (một bước nhịp chung của trận) cùng một lúc, để kết quả không phụ thuộc bên nào được xử trước. Combat không giữ máu: bên sở hữu giữ.

## Contracts

### CombatSource
- Enum: ai ra đòn — `Hero`, `Enemy`, `Castle`.
- Dùng: các feature ra đòn (Hero, Enemy, Castle) ghi vào `CombatHit.Source`.

### CombatHit
- Struct chỉ đọc: một đòn trúng, chốt lúc ra đòn. Bên ra đòn chọn mọi bên bị trúng; đòn vùng là một `CombatHit` cho mỗi bên bị trúng.
- `Receiver`: số của chủ bên bị trúng, do `ICombat.AddReceiver` trả về.
- `TargetKey`: bên bị trúng, do chủ của nó đặt tên.
- `Damage`: số thực, Combat không làm tròn.
- `Source`: `CombatSource` của bên ra đòn.
- Dùng: Hero, Enemy, Castle (ra đòn qua `ICombat.Hit`).

### ICombat
- Bộ phân xử nhịp giao chiến.
- `AddReceiver(receiver)`: thêm một chủ của các bên nhận đòn; trả về số mà đòn vào nó mang ở `CombatHit.Receiver`.
- `AddActor(actor)`: thêm một thứ di chuyển và đánh trên sân.
- `Hit(in hit)`: một đòn trúng. Ra trong nhịp thì thuộc nhịp đó; ra giữa hai nhịp (kĩ năng người chơi bấm) thì thuộc nhịp sau.
- `TickDone`: báo cuối mỗi nhịp, sau khi mọi chủ đã báo các bên bị hạ.
- Dùng: Hero, Enemy, Castle.

### ICombatActor
- Thứ di chuyển và đánh trên sân; Combat gọi theo hai phần mỗi nhịp.
- `Aim(stepSeconds)`: phần đầu — di chuyển, chọn mục tiêu theo trạng thái đầu nhịp.
- `Strike(stepSeconds)`: phần hai, sau khi mọi actor đã Aim — ra đòn qua `ICombat.Hit`.
- Hiện thực: Hero, Enemy, Castle.

### ICombatReceiver
- Chủ của các bên nhận đòn. Combat hỏi ai còn nhận đòn, giao một tổng mỗi bên, rồi để chủ báo bên bị hạ.
- `CanBeHit(key)`: false khi bên không còn nhận đòn (đã hạ, đang hiện lại khi hồi sinh, bị đánh bại, bị phá). Hỏi trước khi trừ máu của nhịp; đòn vào bên false bị bỏ.
- `TakeTotal(key, total)`: trừ tổng sát thương của nhịp, một lần mỗi bên mỗi nhịp. Chưa báo gì.
- `ReportFallen()`: gọi sau khi mọi chủ bị trúng nhịp này đã trừ xong; báo mỗi bên có máu từ trên 0 về 0, một lần mỗi bên.
- Hiện thực: Hero, Enemy, Castle.

## Bên trong
- `CombatTick`, `CombatLoop` (Internal): gom đòn, bỏ đòn vào bên không nhận, cộng tổng, giao, báo hạ, `TickDone`. `CombatParticipant` chạy mỗi bước nhịp chung. `CombatInstaller` đăng ký `ICombat`.

Test: assembly `Game.Combat.Tests` (EditMode).
