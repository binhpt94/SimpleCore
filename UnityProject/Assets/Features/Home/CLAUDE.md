# Home
Tầng: 6 · Features

Màn bắt đầu (`feature-home`): hiện tên game, kỉ lục và nút "Chơi" trong scene Game; chỉ báo người chơi đã bấm Chơi, không tự mở màn khác.

## Contracts

### IHomeScreen
- Dùng để: luồng màn hình hiện, ẩn màn bắt đầu và biết lúc người chơi bấm Chơi.
- `Show()`: hiện màn với kỉ lục hiện tại (đọc `IRunScore.Best`), sẵn nhận một lần chạm Chơi. Gọi trước khi view dựng xong thì màn hiện khi view có.
- `Hide()`: ẩn màn.
- `PlayPressed`: báo khi người chơi chạm Chơi, một lần mỗi lần hiện; màn không tự ẩn hay chuyển.
- Feature dùng: GameFlow.

## Nối
- `HomeInstaller.asset` (`FeatureAssets/Public`): cần `IRunScore` (Run) và `Game.Core.ISessionScene` cài trước; đăng ký `IHomeScreen`, tham gia phiên scene (view dựng ở bước Run, huỷ khi phiên kết thúc). Bản dev cần thêm `Game.Core.ICheatSelection`.
- Số và chữ ở `HomeConfig.asset` (`FeatureAssets/Private`).

## Cheat (chỉ `UNITY_EDITOR || DEVELOPMENT_BUILD`)
- State `with-record` (`HomeCheat.cs`): kỉ lục ép 15 đi qua đường bình thường (bọc `IRunScore`, chỉ thay `Best`); kỉ lục lưu trên máy không đụng.
- Danh sách cheat (partial `Scripts/Internal/HomeInstaller.Cheat.cs`): `HomeInstaller` hiện thực `Game.Core.ICheatStateSource`: `CheatFeatureTitle` "Màn bắt đầu", `CheatStates` theo thứ tự ý của node `feature-home/cheat`, một state `Home/WithRecord` ("Màn bắt đầu đang hiện, đã có kỉ lục").
- Đầu `Install` lấy `ICheatSelection`, thiếu thì lỗi nêu tên. `HasPick` thì bỏ enum `cheat` trên asset, chỉ dựng khi `PickedKey` là `Home/WithRecord` (pick của feature khác hay chơi bình thường: không dựng gì); chưa chọn gì thì dựng state chọn trên asset (asset giao `None`). Dựng state nào cũng gọi `ReportRunning(key)`.

Test: assembly `Game.Home.Tests` (EditMode).
