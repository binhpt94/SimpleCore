# Wallet

Làm chủ số xu của người chơi. Không feature nào khác được giữ hay sửa số xu.

- Contract: `IWalletService.TrySpend(amount)` trả `SpendResult`. Chưa ai cần đọc số xu từ ngoài, nên chưa có contract đọc.
- Cộng xu (`Grant`) và sự kiện `CoinsChanged` đang là `internal`; feature khác cần thì đề xuất thêm vào Contracts.
- Số xu ban đầu: `WalletConfig.asset` trong `FeatureAssets/Private`, lúc chạy chỉ đọc.
- Logic nằm trong `WalletLogic` (C# thuần). Không có static: `WalletCoinsPresenter` đẩy số xu xuống view qua `ICoinsDisplay`.
- Dùng `ISessionScene` (SessionScene): label số xu chỉ được tạo khi phiên tới Running, và bị huỷ khi phiên kết thúc.
- Prefab label: `WalletCoinsLabel.prefab` trong `FeatureAssets/Private`. Phải được cài sau SessionScene.
- `WalletInstaller.asset` trong `FeatureAssets/Public` là thứ duy nhất `_Composition` thấy.

Test: assembly `Game.Wallet.Tests` (EditMode).
