# Enemy
Tầng: 6 · Features

Quái trên sân: thả vào ở mép hai bên, đi về phía thành, dừng ở tầm đánh trước đối thủ, đánh theo nhịp, bị làm chậm, bị hạ thì trả xu.

Vị trí tính bằng đơn vị sân (u), x = 0 ở tâm thành, âm là bên trái. Sân đứng yên ở bước đồng hồ không phải gameplay và sau khi trận dừng.

## Contracts

### EnemySpawn
- Một con quái cần đưa vào sân. Wave tạo và gọi `IEnemyField.Spawn`.
- `Type`: loại quái. `Side`: bên vào. `StatScale`: nhân máu và sát thương gốc (1 = gốc), bên thả quyết định.

### EnemyTarget
- Một con quái còn sống nhìn từ ngoài, tại lúc đọc. Hero, Castle dùng để chọn mục tiêu.
- `Handle`: tên con quái cho `Damage` và combat, không dùng lại trong một lượt. `Side`, `X` (tâm hộp), `HalfWidth`. `SpawnOrder`: nhỏ hơn là vào sân sớm hơn.

### EnemyDefeat
- Báo qua `IEnemyField.Defeated` khi máu một con về 0, một lần mỗi con; con bị gỡ khi dọn sân không báo. Wave dùng để biết dọn sạch đợt.
- `Handle`, `Type`, `Side` của con bị hạ.

### EnemyType
- Các loại quái: `Soldier`, `Wolf`, `Giant`, `Archer`, `Boss`. Mỗi loại có hộp, đòn và chỉ số riêng trong config. Wave dùng.

### EnemySide
- Bên quái đến và đi: `Left`, `Right`; không đổi bên. Wave, Hero dùng.

### IEnemyField
- Quái trên sân. Wave, Hero, Castle dùng.
- `Spawn(spawn)`: đưa một con vào ở điểm thả của bên nó, đủ máu.
- `AliveCount`: số con đã thả chưa bị hạ hay gỡ, kể cả con chưa lên màn hình.
- `GetAlive(into)`: xoá rồi điền mọi con còn sống theo thứ tự thả.
- `Damage(handle, amount)`: trừ máu; con đã mất thì bỏ qua; trả true khi đòn này hạ nó.
- `SlowAll(seconds)`: làm chậm mọi con còn sống; con đang chậm thì tính lại từ đầu.
- `Defeated`: báo một lần mỗi con khi máu về 0, sau khi đã trả xu.
- `AddFoes(foes)`: thêm một nhóm đối thủ cho quái đánh, chủ nhóm gọi trong `Install`.

### IEnemyCombat
- Phía quái của combat chung. Hero, Castle dùng.
- `CombatReceiver`: số combat cấp cho quái khi thêm làm bên nhận; đòn vào quái mang số này và `EnemyTarget.Handle`.
- `AddCombatFoes(foes, combatReceiver)`: thêm nhóm đối thủ đánh qua combat; đòn của quái mang `combatReceiver` và `EnemyFoe.Key`. Chủ nhóm gọi trong `Install`.

### EnemyFoe
- Một đối thủ quái đi tới, dừng lại và đánh, tại lúc đọc. Hero, Castle tạo.
- `Key`: tên đối thủ, chủ nhóm chọn. `X` (tâm hộp), `HalfWidth`.

### IEnemyFoes
- Một nhóm đối thủ (anh hùng, thành). Hero, Castle hiện thực; quái đọc mỗi bước.
- `GetFoes(into)`: xoá rồi điền mọi đối thủ đánh được lúc này.
- `Hit(key, damage)`: một con quái đánh trúng đối thủ `key`.
