# Wallet
Tầng: 6 · Features

Làm chủ số tiền của người chơi trong một lượt. Không feature nào khác được giữ hay sửa số tiền.

- Contracts (cùng một `WalletLogic`):
  - `IWalletService.TrySpend(amount)` trả `SpendResult` (Upgrade). Giá 0 trả `Ok`, giá âm trả `InvalidAmount` và ghi log.
  - `IWalletBalance` (`Coins`, `CoinsChanged`) cho Hud, Upgrade. Chỉ báo khi số dư đổi, và luôn báo khi đặt lại.
  - `IWalletIncome.Add(amount)` cho Enemy, Wave. Cộng 0 bỏ qua, cộng âm từ chối và ghi log, trần 999.999.999. Sau khi lượt kết thúc thì bỏ qua, không log.
- Đặt lại về tiền đầu lượt khi `IRunFlow.RunStarted` (Run), qua `WalletRunReset`. `IRunFlow.BattleStopped` (thành bị phá) khoá mọi lời gọi cộng cho tới `RunStarted` kế tiếp.
- Tiền đầu lượt: `WalletConfig.asset` trong `FeatureAssets/Private` (50), lúc chạy chỉ đọc.
- Logic nằm trong `WalletLogic` (C# thuần). Log qua `IWalletLog`.
- Không có view: hiển thị số tiền là việc của Hud (qua `IWalletBalance`).
- Tham gia `Game.Core.ISessionScene` chỉ để nghe `RunStarted` và `BattleStopped` trong lúc phiên chạy. Phải được cài sau SessionScene và Run.
- `WalletInstaller.asset` trong `FeatureAssets/Public` là thứ duy nhất `_Composition` thấy.

Test: assembly `Game.Wallet.Tests` (EditMode).
