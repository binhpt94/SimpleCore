# Run
Tầng: 6 · Features

Giữ các bước của một lượt trong scene Game: dựng và dọn trận, thành bị phá, chờ 1 giây, thua; đếm điểm (số đợt dọn sạch) và giữ luật kỉ lục. Run không phụ thuộc feature khác ngoài SimClock và Save (`ISavedRecord`: Run quyết giá trị và lúc ghi, không tự đọc ghi xuống máy); từ PROTOTYPE màn đích do GameFlow chọn.

## Contracts
Mục "Dùng bởi" theo gói thiết kế; Wave, Wallet, Hud, Upgrade đã ghi trong `CLAUDE.md` của họ là dùng `IRunFlow`. Feature khác thêm hay bớt chỗ dùng thì báo phiên Run (qua @CodeLead) để sửa mục này.

### RunStep
Enum: lượt đang ở đâu. Chỉ Run đổi bước; màn và popup tự hiện theo bước.
- `Home`, `TeamSelect`: ngoài trận (từ PROTOTYPE Run chỉ dùng `Home` làm bước ngoài trận; màn thật do GameFlow hiện).
- `Playing`: trận đang chạy.
- `Ending`: thành bị phá, trận đã dừng, đang chờ 1 giây trước popup thua. `Lost`: popup thua mở.
- Dùng bởi: Hud, PopupLose.

### RunTeam
Struct chỉ đọc: đội vào trận, mã hero theo thứ tự ô (ô 1 trước).
- `RunTeam(heroCodes)`: chép danh sách, sửa nguồn sau đó không ảnh hưởng.
- `HeroCodes`: mã hero; struct mặc định trả danh sách rỗng. `Count`: số hero.
- Đội hợp lệ: đúng `TeamSize` hero (PROTOTYPE: 3), không mã rỗng, không trùng.
- Dùng bởi: GameFlow, TeamSelect (truyền đội), Hero (dựng đội từ `RunStarted`).

### IRunFlow
Các bước của lượt và tin của trận. Yêu cầu không hợp bước hiện tại, hoặc tới khi đang đổi bước, bị bỏ qua.
- `Step`, `StepChanged(step)`: bước hiện tại; báo sau khi bước đã đổi, kèm bước mới.
- `Team`: đội của lượt đang hoặc vừa chơi; rỗng trước lượt đầu.
- `RunStarted(team)`: lượt mới; mọi feature của trận đặt lại giá trị đầu cho đội này. Báo trong lúc dựng trận, trước `BattleStarted`.
- `BattleStarted`: mọi handler `RunStarted` đã chạy; trận được bắt đầu (đợt 1).
- `BattleStopped`: thành bị phá, lượt kết thúc; không gì trên sân còn hành động.
- `BattleCleared`: rời trận; huỷ mọi thứ trận đã tạo. Báo sau khi đồng hồ chung đã dọn.
- `Play()`, `EnterBattle(team)`: nút ở màn bắt đầu, màn chọn đội (IDEA; từ PROTOTYPE GameFlow dùng `IRunBattle`).
- `Replay()`, `GoHome()`: Chơi lại, Về màn bắt đầu ở popup thua; dọn trận rồi báo `BattleLeft`.
- `Lose()`: thành bị phá. Chốt điểm, `BattleStopped` cùng khung hình, sang `Ending`; sau 1 giây sang `Lost`. Đợt báo dọn sạch cùng khung hình không tính.
- Rời game không đổi bước. Rời game trong 1 giây chờ (`Ending`): thời gian ở ngoài không tính vào 1 giây; quay lại thì chờ nốt phần còn lại rồi popup thua hiện. Nút quay lại của máy: Run không làm gì.
- `CountClearedWave(w)`: đợt w dọn sạch; cộng 1 điểm ở khung hình sau nếu thành không bị phá trong khung hình đó.
- Dùng bởi: Wave, Wallet, Hud, Upgrade, Castle, Enemy, Hero, PopupLose.

### IRunScore
Điểm của lượt và kỉ lục.
- `Score`: số đợt đã dọn sạch của lượt đang hoặc vừa chơi; về 0 khi lượt mới bắt đầu.
- `Best`: kỉ lục của game, đọc qua Save lúc tải phiên (Save báo chưa có, hoặc số âm, thì 0). `IsNewBest`: true khi lượt vừa kết thúc lập kỉ lục mới.
- Kỉ lục đổi thì giao Save ghi ngay lúc chốt, trước popup thua. Save báo ghi thất bại: `Best` vẫn giữ số mới tới khi tắt game, không báo người chơi; Run thử ghi lại một lần mỗi khi game trở lại màn hình và ở lần chốt lượt kế tiếp (kể cả khi lượt đó không phá kỉ lục), ghi được thì thôi.
- `ScoreChanged`: báo sau khi `Score`, `Best` hoặc `IsNewBest` đổi.
- Điểm chốt một lần, lúc thành bị phá.
- Dùng bởi: Hud, PopupLose, Home.

### IRunBattle
Trận nhìn từ điều hướng màn (GameFlow, từ PROTOTYPE).
- `BuildBattle(team)`: dựng trận mới và bắt đầu lượt; trả đúng một trong `BattleBuilt` hoặc `BuildFailed(lí do)` trước khi hàm trả về. Bỏ qua khi đang dựng hoặc đang trong trận. Lỗi thì không để lại gì của trận.
- `CancelBuild()`: GameFlow hết giờ chờ; vứt trận vừa dựng, không báo gì.
- `BattleLeft(choice)`: người chơi đã rời trận; Run đã chốt điểm và dọn trận xong, GameFlow hiện màn kế ngay.
- Dùng bởi: GameFlow.

### RunLeaveChoice
Enum: cách rời trận, kèm trong `IRunBattle.BattleLeft`.
- `PlayAgain` (popup thua: Chơi lại, GameFlow sang màn chọn đội), `BackToStart` (popup thua: Về màn bắt đầu).
- Dùng bởi: GameFlow.

## Bên trong
- Logic C# thuần trong `Scripts/Internal`: `RunFlow` (máy bước), `RunScore` (điểm, kỉ lục, ghi qua `ISavedRecord`, cờ chưa lưu), `RunInterrupts` (rời game, quay lại: giữ các lần chờ; quay lại thì thử ghi kỉ lục chưa lưu), `SimClockRunDelay` (chờ đếm theo bước của đồng hồ chung, đứng yên khi game ở ngoài màn hình qua `IRunWaitHold`).
- View `RunAppSignalsView` chỉ chuyển tín hiệu máy (rời game, quay lại).
- Cần `ISavedRecord` đăng ký trước Run (`SaveInstaller` đứng trước `RunInstaller` trong `Boot.Game.prefab`).
- Số cân bằng ở `RunConfig` trong `FeatureAssets/Private`; installer ở `FeatureAssets/Public`.

Test: assembly `Game.Run.Tests` (EditMode).
