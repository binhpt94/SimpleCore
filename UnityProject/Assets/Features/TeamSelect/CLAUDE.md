# TeamSelect
Tầng: 6 · Features

Màn chọn đội trước mỗi trận: 5 thẻ hero, 3 chỗ trong đội không trùng, nút "Vào trận" sáng khi đủ 3; nhớ đội của lượt vừa vào trận, lần đầu điền đội mặc định.

## Contracts

### ITeamSelectScreen
- Dùng để: luồng màn hình (GameFlow) hiện, ẩn màn chọn đội và nhận thao tác của nó.
- `Show()`: mở màn, 3 chỗ điền từ đội nhớ (chưa có thì đội mặc định Hiệp sĩ, Đấu sĩ rìu, Cung thủ).
- `Hide()`: ẩn màn; chỗ đã chọn mà chưa vào trận bị bỏ, lần mở sau điền lại.
- `SetEnterBattleEnabled(bool)`: khoá (false) hay mở nút "Vào trận" trong lúc dựng trận.
- `ShowBuildError(text)` / `HideBuildError()`: dòng lỗi dựng trận.
- `RememberTeam(heroCodes)`: trận đã dựng cho đội này, nó thành đội nhớ.
- `EnterBattlePressed(team)`: người chơi bấm "Vào trận" với đội đủ 3, mã hero theo thứ tự chỗ (chỗ 1 trước).
- `BackPressed`: người chơi bấm "Quay lại" hay phím back của máy.
- Feature dùng: GameFlow.

## Cài đặt
- `TeamSelectInstaller.asset` trong `FeatureAssets/Public`. Cần `IHeroRoster` (Hero), `ISessionScene`; đăng ký `ITeamSelectScreen`. Có view (tạo ở Run).

## Cheat (chỉ bản dev, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`)
- Installer hiện thực `ICheatStateSource` của Core (partial `TeamSelectInstaller.Cheat.cs`): `CheatFeatureTitle` = "Chọn đội"; `CheatStates` theo thứ tự ý của node `feature-team-select/cheat`: `TeamSelect/FullTeam` "Màn chọn đội đang mở, đội nhớ đã điền đủ 3 chỗ, nút "Vào trận" sáng".
- Đầu `Install` lấy `ICheatSelection` của Core; thiếu thì lỗi nêu tên. `HasPick` thì bỏ cheat chọn trên asset (trường `cheat`), chỉ dựng khi `PickedKey` là của TeamSelect; chưa pick thì dựng state chọn trên asset. Dựng state nào thì gọi `ReportRunning(key)`.
- `FullTeam`: đội nhớ ép Hiệp sĩ, Đấu sĩ rìu, Cung thủ (qua đường nhớ đội thật) rồi mở màn lúc Install.

Test: assembly `Game.TeamSelect.Tests` (EditMode).
