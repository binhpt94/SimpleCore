# SimClock

Nhịp chung và ngẫu nhiên theo seed của trận. Thiết kế: `live/feature-sim-clock` (vòng 4: `clock-step/clock-values`, `seeded-random/random-values`).

## Dùng từ feature khác (Contracts)
- `ISimClock.OnStep("<Owner>", step => ...)`: gọi một lần mỗi bước, theo thứ tự owner trong config (hiện là `Wave, Enemy, Hero, Castle, Combat, Run`). Owner không có trong thứ tự thì lỗi ngay. Bước có `Gameplay == false` (tạm dừng, chờ hồi sinh) thì feature tự đứng yên.
- Mọi chờ giờ đếm bằng `GameplaySeconds` hoặc `GameplayStepCount`, không đọc thời gian khung hình. Chờ t giây hết ở bước đầu tiên mà giờ đã đếm đạt hoặc vượt t.
- `ISimClock.Enqueue("<Owner>", () => ...)`: thao tác của người chơi, áp ở đầu bước kế tiếp đúng thứ tự đưa vào; feature nhận tự từ chối theo luật của nó.
- `ISimRandom.Stream("Wave")`: một dòng theo tên (id hoặc tên trong bảng Streams, không phân biệt hoa thường). Tên không có trong bảng thì lỗi. Hiệu ứng chỉ để nhìn không xin dòng ở đây.
- `ISimRunControl` (chủ lượt): `SetUpRun` (đặt lại bước, thời gian dư, hàng chờ, chọn seed, đặt lại dòng) → `StartRun` (bắt đầu chạy bước, ghi seed đúng một lần) → `SetGameplay(false/true)` (tạm dừng, chờ hồi sinh; không đặt lại gì) → `ClearRun` (dọn hàng chờ, thời gian dư, dòng).

## Bên trong
- `SimClockService` (C# thuần): logic nhịp, lượt và dòng. `SimClockParticipant` tham gia phiên scene, chạy vòng UniTask ở `PreUpdate` gọi `Advance(Time.unscaledDeltaTime)`.
- Config `FeatureAssets/Private/SimClockConfig.asset`: ClockParams, SeedParams, thứ tự owner, bảng Streams. Cách chạy đọc lại mỗi lần `SetUpRun`.
- Bản dev (`UNITY_EDITOR || DEVELOPMENT_BUILD`): `SetSpeed` (1, 2, 4, 8, 16), `Freeze`, `StepOnce`, `RunSteps(N)` trong Internal, không mở contract. Seed dev lấy đúng `devSeed` của config mỗi lượt và in ra console; bản phát hành bốc seed ngẫu nhiên và ghi vào log.
- Installer `FeatureAssets/Public/SimClockInstaller.asset`: cài ngay sau SessionScene, trước mọi feature gameplay.

Test: assembly `Game.SimClock.Tests` (EditMode).
