# Wave

Điều phối các đợt quái của một trận: lập đợt từ số đợt, thả quái hai bên, nhận ra lúc dọn sạch, trả thưởng, lúc thở 3 giây rồi sang đợt sau.

- Contract: `IWaveProgress` (cho Hud):
  - `CurrentWave` là đợt đang đánh, hoặc đợt sắp tới trong lúc thở.
  - `CurrentWaveChanged` báo lúc đầu lượt (1) và ngay khi dọn sạch (w+1).
  - `WaveStarted(w)` báo khi đợt bắt đầu thả quái. `WaveCleared(w)` báo một lần mỗi đợt, sau khi đã trả thưởng.
- Dùng: `IRunFlow` (Run), `IEnemyField` (Enemy), `IWalletIncome` (Wallet). Run không nghe Wave.
- Nhịp: `RunStarted` đặt lại về đợt 1. `BattleStarted` bắt đầu đợt 1. `BattleStopped`/`BattleCleared` ngừng thả quái. `IsPaused` đóng băng mọi đồng hồ.
- Dọn sạch đợt w, đúng thứ tự: `IWalletIncome.Add(20 + 5w)`, `IRunFlow.CountClearedWave(w)`, `WaveCleared(w)`, `CurrentWave = w+1`, rồi thở 3 giây và `WaveStarted(w+1)`.
- Việc xét dọn sạch được hoãn tới bước cập nhật kế tiếp: thành bị phá trong cùng bước thì không tính.
- Hệ số chỉ số `1 + 0.10×(w−1)` do Wave tính và đưa vào `EnemySpawn.StatScale`.
- Logic C# thuần trong `Scripts/Internal`:
  - `WaveFormula`: số quái, nhịp, hệ số, số con từng loại.
  - `SpawnOrder`: xáo loại, Trùm cuối, mỗi bên một nửa, tối đa 3 con liền một bên.
  - `WaveDirector`: máy trạng thái, chạy qua `Tick`.
  - Đồng hồ khung hình qua `IWaveClock`: `UnityWaveClock` là một vòng UniTask, không dùng MonoBehaviour.
- Số cân bằng: `WaveConfig.asset` trong `FeatureAssets/Private`. `useArcher`/`useBoss` đang tắt (mức IDEA: chỗ của chúng dồn cho Lính); bật khi Enemy có đủ 5 loại.
- `WaveInstaller.asset` trong `FeatureAssets/Public`. Cài sau SessionScene, Run, Wallet, Enemy. Không có view.
- Cheat (chỉ bản dev, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`): contract `IWaveCheat.StartWave(w)` cho Hud dựng state mid-battle. `WaveDirector` hiện thực; installer đăng ký cùng cờ dev. Chỉ chạy khi trận đang chạy (đang thả hoặc đang thở). Bỏ phần còn lại của đợt hoặc lúc thở, rồi `CurrentWaveChanged(w)`, `WaveStarted(w)`, con đầu ra ngay. Quái còn trên sân vẫn tính vào số còn sống của đợt mới. Đợt bị bỏ qua không có thưởng và không tính điểm.

Test: assembly `Game.Wave.Tests` (EditMode).
