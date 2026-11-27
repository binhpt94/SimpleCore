# Wave
Tầng: 6 · Features

Điều phối các đợt quái của một trận: lập đợt từ số đợt, thả quái hai bên, nhận ra lúc dọn sạch, trả thưởng, lúc thở 3 giây rồi sang đợt sau.

- Contract: `IWaveProgress` (cho Hud):
  - `CurrentWave` là đợt đang đánh, hoặc đợt sắp tới trong lúc thở.
  - `CurrentWaveChanged` báo lúc đầu lượt (1) và ngay khi dọn sạch (w+1).
  - `WaveStarted(w)` báo khi đợt bắt đầu thả quái. `WaveCleared(w)` báo một lần mỗi đợt, sau khi đã trả thưởng.
- Dùng: `IRunFlow` (Run), `IEnemyField` (Enemy), `IWalletIncome` (Wallet), `ISimClock` và `ISimRandom` (SimClock). Run không nghe Wave.
- Nhịp chung: `WaveParticipant` đăng kí `ISimClock.OnStep("Wave", WaveDirector.Step)`; Wave chạy đầu bước nên thấy tin bị hạ và kết quả lượt của bước trước. Không đọc thời gian khung hình. Mọi chờ (nhịp thả, lúc thở) chỉ đếm bước `Gameplay`; bước không gameplay thì đứng yên.
- Ngẫu nhiên: xáo loại và gán bên lấy từ `ISimRandom.Stream("Wave")` qua `StreamWaveRandom`; không tự tạo bộ ngẫu nhiên. Danh sách thả lập một lần đầu đợt, con chờ trần giữ nguyên loại và bên.
- Mốc: `RunStarted` đặt lại về đợt 1. `BattleStarted` hẹn đợt 1 ở bước gameplay đầu tiên (con đầu ra trong bước đó). `BattleStopped`/`BattleCleared` ngừng thả quái.
- Dọn sạch đợt w, ba bước nhịp (người quyết 2026-11-27, phương án a):
  - Bước k: con cuối bị hạ sau khi đã ra hết. Wave chỉ ghi nhận.
  - Bước k+1, chỗ của Wave: trận còn chạy thì `IRunFlow.CountClearedWave(w)`. Run giữ tin tới cuối bước này và bỏ nếu thành bị phá trong bước đó.
  - Bước k+2, chỗ của Wave: trận còn chạy (thành không bị phá ở k+1) thì `IWalletIncome.Add(20 + 5w)`, `WaveCleared(w)`, `CurrentWave = w+1`, vào lúc thở. Thành bị phá ở bước k hay k+1 thì không thưởng, không lúc thở.
- Lúc thở đếm từ bước sau bước trả thưởng; hết 3 giây thì `WaveStarted(w+1)`.
- Hệ số chỉ số `1 + 0.10×(w−1)` do Wave tính và đưa vào `EnemySpawn.StatScale`.
- Logic C# thuần trong `Scripts/Internal`:
  - `WaveFormula`: số quái, nhịp, hệ số, số con từng loại.
  - `SpawnOrder`: xáo loại, Trùm cuối, mỗi bên một nửa, tối đa 3 con liền một bên.
  - `WaveDirector`: máy trạng thái, chạy qua `Step(SimStep)`; báo `Stepped` cuối mỗi bước.
- Số cân bằng: `WaveConfig.asset` trong `FeatureAssets/Private`, đang ở giá trị PROTOTYPE: `useArcher`/`useBoss` bật (đủ 5 loại: Cung thủ quái từ đợt 7, Trùm mỗi 10 đợt, con cuối). Mức IDEA đã tắt hai cờ này (chỗ của chúng dồn cho Lính).
- Đếm cho SessionScene (đề xuất `2026-11-25-session-usage-tracking`): `WaveParticipant` giữ một `TrackJob` cho đăng kí bước suốt phiên và một `TrackJob` cho đồng hồ thả hoặc lúc thở khi `WaveDirector.IsRunning` (báo qua `RunningChanged`). Wave không có view. Dọn phiên thì mọi số về 0.
- `WaveInstaller.asset` trong `FeatureAssets/Public`. Cài sau SessionScene, SimClock, Run, Wallet, Enemy. Không có view.
- Cheat (chỉ bản dev, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`): contract `IWaveCheat.StartWave(w)` cho Hud dựng state mid-battle. `WaveDirector` hiện thực; installer đăng ký cùng cờ dev. Chỉ chạy khi trận đang chạy (đang thả hoặc đang thở). Bỏ phần còn lại của đợt hoặc lúc thở, rồi `CurrentWaveChanged(w)`, `WaveStarted(w)`, con đầu ra ngay. Quái còn trên sân vẫn tính vào số còn sống của đợt mới. Đợt bị bỏ qua không có thưởng và không tính điểm.

Test: assembly `Game.Wave.Tests` (EditMode).
