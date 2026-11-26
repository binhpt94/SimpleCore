# Upgrade
Tầng: 6 · Features

Cửa hàng nâng cấp trong trận: 6 mục (hero chỗ 1, 2, 3, rồi máu thành, sức đánh thành, kĩ năng thành), mọi mục vào trận ở cấp 1. Thiết kế: `feature-upgrade` trong `Management/game-design/live`.

- Contract: `IUpgradeShop` (`Count`, `Get`, `Buy`, `ItemsChanged`), `UpgradeItem`, `UpgradeKind`, `PurchaseResult`. `feature-hud-upgrade` dùng để lập nút.
- Lời bấm mua của người chơi: `IUpgradeOrders.Order(index)` đưa vào hàng chờ `ISimClock.Enqueue` (owner `Upgrade`), áp ở đầu bước kế tiếp bằng chính `Buy` (kiểm lại trận đã hết, tiền), rồi phát `OrderApplied(index, PurchaseResult)`. Lệnh còn từ lượt trước bị bỏ, không báo. `Buy` giữ làm lệnh mua ngay cho cheat dev và test. Cùng object với `IUpgradeShop`.
- Dùng: `IRunFlow` (lập lại danh sách khi `RunStarted`, mua được từ `BattleStarted` tới `BattleStopped`, chỉ khi `Step == Playing`), `IWalletService` trừ tiền, `IWalletBalance` để tính `CanBuy`, `IHeroSquad.ApplyLevel`, `ICastleUpgrades.ApplyLevel`.
- Không giữ chỉ số: chỉ báo "mục X lên cấp L"; Hero và Castle tự tính mức tăng.
- Logic: `UpgradeShopLogic` (C# thuần). Giá: `PriceCurve` (làm tròn 6 chữ số rồi làm tròn lên).
- Số liệu: `UpgradeConfig.asset` trong `FeatureAssets/Private`. `UpgradeInstaller.asset` trong `FeatureAssets/Public` là thứ duy nhất `_Composition` thấy.
- Không có view (thanh nút thuộc `HudUpgrade`). Cài sau SessionScene, Run, Wallet, Hero, Castle.

Test: assembly `Game.Upgrade.Tests` (EditMode).
