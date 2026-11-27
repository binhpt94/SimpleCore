# HudUpgrade
Tầng: 6 · Features
Thanh nâng cấp ở mép dưới màn hình trận: 6 nút (3 hero, 3 thành), một chạm là mua. Thiết kế: `live/feature-hud-upgrade` (cheat ở `feature-hud-upgrade/cheat`).

## Cách chạy
- `HudUpgradeInstaller` (`FeatureAssets/Public/HudUpgradeInstaller.asset`) cài sau SessionScene, Run, Wallet, Hero, Upgrade; cần `ISessionScene`, `IUpgradeShop`, `IUpgradeOrders`, `IHeroRoster`, `IRunFlow`, `IHeroDownState`, thiếu thì lỗi nêu tên.

## Cheat (chỉ `UNITY_EDITOR || DEVELOPMENT_BUILD`)
- Installer hiện thực `ICheatStateSource` của Core (partial `Scripts/Internal/Cheat/HudUpgradeInstaller.Cheat.cs`): `CheatFeatureTitle` = "Thanh nâng cấp"; một state `HudUpgrade/MixedButtons` ("Giữa trận, đủ tiền mua năm mục, thiếu tiền cho một mục"), theo thứ tự ý của node cheat.
- State `MixedButtons` (`UpgradeBarCheat.cs`): khi trận bắt đầu, mua qua cửa hàng tới cấp 3, 5, 1, 2, 4, 3 rồi đưa tiền về 120; số liệu ở `HudUpgradeConfig`.
- Đầu `Install` lấy `ICheatSelection` của Core từ registry; thiếu thì lỗi nêu tên `ICheatSelection`. `HasPick` thì bỏ `cheatState` trên config asset, chỉ dựng khi `PickedKey` là `HudUpgrade/MixedButtons` (pick của feature khác hay chơi bình thường: không dựng gì); chưa chọn thì theo `cheatState`. Mỗi lần dựng state gọi `ReportRunning(key)`.
- Config dùng chung phải để `cheatState` = `None` (có test kiểm).
