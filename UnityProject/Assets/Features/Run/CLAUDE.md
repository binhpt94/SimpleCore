# Run
Tầng: 6 · Features

Giữ các bước của một lượt trong scene Game: dựng và dọn trận, tạm dừng, thành bị phá, chờ 1 giây, hồi sinh một lần, thua; đếm điểm (số đợt dọn sạch) và lưu kỉ lục. Run không phụ thuộc feature khác ngoài SimClock; từ PROTOTYPE màn đích do GameFlow chọn.

## Contracts
Mục "Dùng bởi" theo gói thiết kế; Wave, Wallet, Hud, Upgrade đã ghi trong `CLAUDE.md` của họ là dùng `IRunFlow`. Feature khác thêm hay bớt chỗ dùng thì báo phiên Run (qua @CodeLead) để sửa mục này.

### RunStep
Enum: lượt đang ở đâu. Chỉ Run đổi bước; màn và popup tự hiện theo bước.
- `Home`, `TeamSelect`: ngoài trận (từ PROTOTYPE Run chỉ dùng `Home` làm bước ngoài trận; màn thật do GameFlow hiện).
- `Playing`: trận đang chạy. `Paused`: trận đứng yên, popup tạm dừng mở.
- `Ending`: thành bị phá (không còn hồi sinh), trận đã dừng, đang chờ 1 giây trước popup thua. `Lost`: popup thua mở.
- `ReviveWait`: thành bị phá lần đầu trong lượt, trận đứng yên, chờ 1 giây trước popup hồi sinh. `ReviveOffer`: popup hồi sinh mở. `Reviving`: đã xem xong quảng cáo, thành đang đứng dậy, chờ `IRunRevive.ReviveShown`.
- Dùng bởi: Wave (giữ tin dọn sạch ở các bước hồi sinh), Hud, các popup lose, pause, revive.

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
- `BattleStopped`: lượt kết thúc (thua, bỏ lượt) hoặc hồi sinh bị từ chối; không gì trên sân còn hành động. Lần bị phá đầu (có hồi sinh) không báo.
- `BattleCleared`: rời trận; huỷ mọi thứ trận đã tạo. Báo sau khi đồng hồ chung đã dọn.
- `IsPaused`: true ở `Paused`, `ReviveWait`, `ReviveOffer`, `Reviving`.
- `Play()`, `EnterBattle(team)`: nút ở màn bắt đầu, màn chọn đội (IDEA; từ PROTOTYPE GameFlow dùng `IRunBattle`).
- `Pause()`: nút tạm dừng, nút quay lại của máy, rời game; chỉ có tác dụng ở `Playing`, áp ở đầu bước kế tiếp của đồng hồ chung. `Resume()`: Tiếp tục ở popup tạm dừng.
- `Abandon()`: đồng ý bỏ lượt; chốt điểm như thua, dọn trận, báo `IRunBattle.BattleLeft(Abandon)`, không popup thua.
- `Replay()`, `GoHome()`: Chơi lại, Về màn bắt đầu ở popup thua; dọn trận rồi báo `BattleLeft`.
- `Lose()`: thành bị phá. Lần đầu trong lượt (khi có hồi sinh): sang `ReviveWait`, trận đứng yên, không chốt điểm. Còn lại: chốt điểm, `BattleStopped` cùng khung hình, sang `Ending`; sau 1 giây sang `Lost`. Đợt báo dọn sạch cùng khung hình không tính.
- Rời game trong 1 giây chờ (`ReviveWait`, `Ending`): không tạm dừng; thời gian ở ngoài không tính vào 1 giây; quay lại thì chờ nốt phần còn lại rồi popup hồi sinh (lần bị phá đầu) hoặc popup thua hiện.
- `CountClearedWave(w)`: đợt w dọn sạch; cộng 1 điểm ở khung hình sau nếu thành không bị phá trong khung hình đó. Ở `ReviveWait` tin được giữ: hồi sinh thì tính, thua thì bỏ.
- Dùng bởi: Wave, Wallet, Hud, Upgrade, Castle, Enemy, Hero, các popup lose và pause.

### IRunRevive
Hồi sinh, mỗi lượt một lần. Cùng object với `IRunFlow`.
- `ReviveStarted`: lượt vào `Reviving`; thành đứng dậy rồi gọi `ReviveShown`. Trận chưa bị dừng nên không gì đặt lại.
- `LostAfterFailedRevive`: true ở `Lost` khi thua vì không xem hết quảng cáo (popup thua ghi lí do).
- `Revive()`: xem xong quảng cáo, sang `Reviving`. `DeclineRevive()`: chọn kết thúc, thua ngay không chờ thêm. `FailRevive()`: quảng cáo không xem hết hay không tải được, như từ chối và bật `LostAfterFailedRevive`. Chỉ lần chọn đầu ở `ReviveOffer` có tác dụng.
- `ReviveShown()`: thành đã đứng dậy; trận chạy tiếp từ chỗ dừng, tin dọn sạch đang giữ được tính.
- Dùng bởi: PopupRevive, Castle.

### IRunScore
Điểm của lượt và kỉ lục.
- `Score`: số đợt đã dọn sạch của lượt đang hoặc vừa chơi; về 0 khi lượt mới bắt đầu.
- `Best`: kỉ lục đã lưu trên máy (đọc lúc tải phiên). `IsNewBest`: true khi lượt vừa kết thúc lập kỉ lục mới.
- `ScoreChanged`: báo sau khi `Score`, `Best` hoặc `IsNewBest` đổi.
- Điểm chốt một lần khi lượt thật sự kết thúc (thua không hồi sinh, bỏ lượt).
- Dùng bởi: Hud, PopupLose, Home.

### IRunBattle
Trận nhìn từ điều hướng màn (GameFlow, từ PROTOTYPE).
- `BuildBattle(team)`: dựng trận mới và bắt đầu lượt; trả đúng một trong `BattleBuilt` hoặc `BuildFailed(lí do)` trước khi hàm trả về. Bỏ qua khi đang dựng hoặc đang trong trận. Lỗi thì không để lại gì của trận.
- `CancelBuild()`: GameFlow hết giờ chờ; vứt trận vừa dựng, không báo gì.
- `BattleLeft(choice)`: người chơi đã rời trận; Run đã chốt điểm và dọn trận xong, GameFlow hiện màn kế ngay.
- Dùng bởi: GameFlow.

### RunLeaveChoice
Enum: cách rời trận, kèm trong `IRunBattle.BattleLeft`.
- `PlayAgain` (popup thua: Chơi lại, GameFlow sang màn chọn đội), `BackToStart` (popup thua: Về màn bắt đầu), `Abandon` (popup tạm dừng: bỏ lượt, về màn bắt đầu).
- Dùng bởi: GameFlow.

## Bên trong
- Logic C# thuần trong `Scripts/Internal`: `RunFlow` (máy bước), `RunScore`, `RunInterrupts` (rời game, quay lại, nút quay lại), `SimClockRunDelay` (chờ đếm theo bước của đồng hồ chung, đứng yên khi game ở ngoài màn hình qua `IRunWaitHold`).
- View `RunAppSignalsView` chỉ chuyển tín hiệu máy (rời game, quay lại, nút quay lại).
- Số cân bằng ở `RunConfig` trong `FeatureAssets/Private`; installer ở `FeatureAssets/Public`.

Test: assembly `Game.Run.Tests` (EditMode).
