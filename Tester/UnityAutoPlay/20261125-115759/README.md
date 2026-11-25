# IDEA mot luot day du 12 feature va choi lai 3 lan
Tester: UnityAutoPlay · Đợt: 20261125-115759

## Mục đích
@Manager giao ngày 2026-11-25, mức IDEA, mục tiêu là chơi được game ở IDEA. Cần xác nhận một lượt IDEA chạy đúng thiết kế đã chốt sau các thay đổi hôm nay:
- vòng đời phiên chuyển sang Core;
- ví bỏ qua tiền tới sau khi lượt kết thúc;
- scene Game ở IDEA có 12 feature, đã có Battlefield (nền chiến trường).

Đồng thời kiểm các yêu cầu ghi `play` của Wave, Hero, Castle, Enemy, Popup, PopupLose, SessionScene, Run, SceneFlow, HudUpgrade, để @CodeLead verify.

## Test gì
- Feature: Run, Hud, HudUpgrade, PopupLose, SceneFlow, Battlefield, Wave, Hero, Castle, Enemy, Wallet, Upgrade, dịch vụ Popup và SessionScene.
- Node `live` chính:
  - `feature-scene-flow/scene-list/boot-loading`
  - `feature-run/run-flow/flow-rules`
  - `feature-run/run-start/start-values`
  - `feature-run/run-start/replay-checks`
  - `feature-hud/hud-layout/layout-spec`
  - `feature-hud-upgrade/bar-layout/bar-buttons`
  - `feature-hud-upgrade/button-state/button-display`
  - `feature-castle/castle-skills/skill-values`
  - `feature-popup-lose/lose-actions/actions-spec`
- Gói thiết kế đã đọc: `pack-Run-deb32f2858a8.md`, `pack-SceneFlow-3d6315902282.md`, `pack-Hud-0c915ed45dcd.md`, `pack-HudUpgrade-6a6898b505fa.md`, `pack-PopupLose-69f796336a0c.md`.
- Scene: mở `Assets/_Composition/Loading.unity` rồi Play, game tự sang `Game.unity`.
- Không dùng state cheat của feature. Để thành bị phá nhanh, `execute_code` gọi `CastleBody.ForceHealth(1)`, cùng hàm mà `ICastleCheat.SetHealth` dùng; sau đó để quái tự đánh vỡ thành.

## Môi trường
- Commit HEAD lúc mở phiên: 2bd8f00, cộng các thay đổi chưa commit của hôm nay (guard chặn lệnh git trong phiên tester).
- Unity 6000.3.10f1 (`UnityProject@638c2683`), Play trong Editor (bản dev: cờ `UNITY_EDITOR` bật).
- Nền tảng Android. Game view chọn `1920x1080 Landscape` cho ảnh 16:9 và `800x480 Landscape` cho ảnh 5:3. Ảnh đầu (01) bị chụp ở `1280x720 Portrait` vì tester chọn nhầm chỉ số cỡ; đã trả về `1920x1080 Landscape`, đúng cỡ trước khi test.
- Thao tác người chơi giả lập bằng `execute_code`:
  - bấm nút qua `ExecuteEvents.pointerClickHandler`;
  - đọc trạng thái bằng reflection từ `SessionSceneDriver` → phiên → các participant;
  - chụp ảnh bằng `ScreenCapture` vào `images/`.
- Không ghi file hay asset nào khác. Riêng game tự lưu kỉ lục vào PlayerPrefs `Run/Best` = 8 trên máy này.

## Các bước
Viết tắt: MĐ là mong đợi theo thiết kế, TT là thực tế thấy.

1. **Loading.**
   - MĐ: tên game dạng chữ, thanh tiến độ, không nút, hiện ít nhất 1 giây, rồi vào thẳng trận (không có màn bắt đầu hay chọn đội).
   - TT:
     - Ảnh 01 có tên "Thủ thành anh hùng" và thanh tiến độ, không có nút.
     - Sau Loading vào thẳng trận với đội mặc định, không có màn trống.
     - Không đo được thời gian "ít nhất 1 giây" và việc thanh "nhảy theo từng feature": mỗi lệnh unity-mcp trễ khoảng 3 giây sau Play, lúc đó lớp che đã ở 100%.
     - Ảnh Loading 16:9 cũng không kịp chụp (ảnh 11 đã là trận).
   - Kết quả: đạt phần thấy được.
1b. **Nền.**
   - MĐ: trời, đồi xa, dải đất cỏ, decor hai bên, đều nằm dưới thành, quái, hero; HUD không che mặt đất.
   - TT:
     - Có trời hai tầng xanh, dải đồi xanh, dải cỏ và đất nâu, decor đá và bụi cỏ hai bên thành.
     - Thành, hero, quái vẽ trên nền.
     - Mặt cỏ nằm khoảng y 840–880; dải trên HUD kết thúc ở y 140; thanh nâng cấp bắt đầu ở y 920.
   - Kết quả: đạt (ảnh 02, 10).
2. **Đầu lượt.**
   - MĐ: thành 1000/1000, tiền 50, mọi cấp 1, Đợt 1, điểm 0, đội Hiệp sĩ, Đấu sĩ rìu, Cung thủ, Sấm sét sẵn sàng.
   - TT lượt 1: không đọc kịp ngay khung đầu, vì trận đã chạy khoảng 17 giây khi lệnh đầu tới. Ảnh 02 ở đợt 2 vẫn thấy 1000/1000 và mọi nút "Cấp 1".
   - TT lượt 2, 3, 4: đọc ngay sau khi bấm Chơi lại, lần nào cũng đúng.
     - Step Playing, điểm 0, kỉ lục giữ 8, tiền 50, thành 1000/1000, đợt 1.
     - 3 hero cấp 1, đủ máu: 300, 240, 120.
     - Đội gồm knight, axeman, archer; Thunder sẵn sàng.
     - Thanh nâng cấp "Cấp 1" với giá 30/30/30/40/40/60.
   - Kết quả: đạt (ảnh 08, 10).
3. **HUD.**
   - MĐ: không có nút tạm dừng; thanh 6 nút không đè cụm kĩ năng; ảnh ở 16:9 và 5:3.
   - TT 16:9:
     - Không có nút tạm dừng.
     - 6 nút chia 2 nhóm: hero x 50–670, thành x 740–1360, hai nhóm cách 70 px, nút cùng nhóm cách 10 px.
     - Nút Sấm sét ở x 1420–1560, cách thanh 60 px.
   - TT 5:3 (800x480): thanh co lại, nút 6 kết thúc ở x 578 và nút Sấm sét bắt đầu đúng x 578. Hai bên chạm nhau, không chồng, nhưng mất khoảng hở 40 px của frame (lỗi 1).
   - Kết quả: đạt, kèm lỗi nhẹ (ảnh 02, 05).
4. **Quái.**
   - MĐ: chỉ có Lính, Sói, Khổng lồ; không có Cung thủ quái, không có Trùm; hạ quái được cộng tiền.
   - TT, danh sách thả của đợt:
     - Đợt 4: 14 con (Lính, Sói), mỗi bên 7.
     - Đợt 7: Lính 12, Sói 5, Khổng lồ 3 (đúng 6 + 2×7 = 20), không có Cung thủ quái.
     - Đợt 9 thấy Khổng lồ; không thấy Trùm (chưa tới đợt 10).
   - TT về tiền: tiền tăng liên tục khi quái bị hạ (130 → 195 → 393 …).
   - Kết quả: đạt.
5. **Mua 1 nâng cấp hero và 1 nâng cấp thành.**
   - MĐ: tiền trừ đúng giá, có phản hồi phóng to.
   - TT:
     - Hero 1: 393 → 363 (giá 30), nút thành "Cấp 2 | 41".
     - Máu thành: 363 → 323 (giá 40), máu 871/1000 → 1021/1150 (+150).
     - Thêm Sức đánh (40), Hero 2, 3 (30), Kĩ năng (60 ở lượt 2): đều trừ đúng giá.
     - Coroutine nảy (`UpgradeBarView.BounceRoutine`, 1.15 lần trong 0.2 giây) có chạy sau mỗi lần mua thành công. Nhưng không bắt được khung hình phóng to qua unity-mcp: khung đầu sau lệnh có `unscaledDeltaTime` lớn hơn 0.2 giây.
   - Kết quả: đạt phần trừ tiền; phản hồi phóng to cần người xem bằng mắt (ảnh 03).
6. **Sấm sét.**
   - MĐ: có hiệu ứng, có hồi chiêu.
   - TT lần 1: không có quái trong vùng 7.5u, vẫn tính là dùng; hồi chiêu 20 giây, nút hiện lớp phủ và số "20" (ảnh 04).
   - TT lần 2: 4 quái trong vùng, mỗi con mất đúng 80 máu (2 con bị hạ); sinh 4 object hiệu ứng `Thunder` dưới `CastleView` cùng lớp phủ hồi chiêu.
   - Kết quả: đạt (ảnh 06; hình lóe 0.3 giây không kịp lên ảnh).
7. **Thành bị phá.**
   - MĐ: popup thua sau 1 giây, hiện điểm và kỉ lục, chỉ có nút Chơi lại.
   - TT:
     - Từ Ending tới Lost và popup hiện mất 1.085 giây.
     - Popup: "Thành đã bị phá", "Trụ được 8 đợt", "Kỉ lục: 8 đợt", "Kỉ lục mới!", chỉ có nút "Chơi lại".
     - Thanh nâng cấp ẩn; quái đứng yên (X không đổi); hồi chiêu Sấm sét đứng ở 0.74.
     - Chạm nền che ở góc màn và vùng ngoài khung popup đều không đóng popup.
     - Lượt 2 thua với điểm 1, kỉ lục 8, không có dòng "Kỉ lục mới!".
   - Kết quả: đạt (ảnh 07, 09).
8. **Chơi lại 3 lần liền** (lượt 1 → 2 → 3 → 4).
   - MĐ: mỗi lượt về đúng giá trị đầu lượt, không dồn view hay quái thừa, console không lỗi.
   - TT:
     - Popup đóng ngay khung kế tiếp; đầu lượt không còn popup nào (PopupLayer chỉ còn gốc).
     - Số Transform theo gốc ở đầu lượt 3 và đầu lượt 4 bằng nhau: Battlefield 14, Heroes 10, CastleView 2, HudView 16, UpgradeBar 32, PopupLayer 1; MonoBehaviour 80 = 80.
     - Lượt 2 đo sau vài giây cũng bằng các số này.
     - EnemyStage chỉ khác theo số quái đang sống (5–9).
     - 4 hiệu ứng Thunder của lượt 1 đã bị huỷ (CastleView về 2).
     - Console: 0 lỗi, 0 cảnh báo suốt đợt.
   - Kết quả: đạt.
9. **Rời game.**
   - MĐ: Stop Play giữa lúc Loading, khi popup thua đang mở, và ngay lúc chuyển scene, console không lỗi dispose.
   - TT:
     - Stop Play khi popup thua lượt 4 đang mở: 0 lỗi; PlayerPrefs `Run/Best` = 8 còn sau khi Stop.
     - Stop Play ở khung thứ 0.07 giây sau Play: scene Game đã nạp, lớp che Loading còn hiện, đang trong lúc chuyển và chờ thời gian ít nhất. Kết quả 0 lỗi.
     - Không Stop được sớm hơn, khi scene Loading còn đang tải: Play có domain reload, lệnh đầu tới sau khoảng 3 giây.
   - Kết quả: đạt phần làm được.

### Mục play theo yêu cầu (cho @CodeLead verify)
Đạt:
- `feature-scene-flow/scene-list/boot-loading#game-first-screen`: sau Loading thấy ngay trận với đội mặc định, view trận đã có.
- `feature-session-scene/session-clean/clean-checks#after-dispose-zero`, phần GameObject và console:
  - sau khi sang Game mọi gốc đều thuộc scene Game, không còn object nào của scene Loading;
  - console không lỗi dispose;
  - phần Profiler và UniTask Tracker chưa kiểm.
- `feature-wave/spawning/spawn-order#spawn-point`: quái mới ở X khoảng ±15.5 đến ±16.7 ngoài mép màn rồi đi vào thành.
- `feature-wave/spawning/spawn-order#edge-mua-nang-cap-dung-ki-nang-thanh-giua`: mua 6 lần và dùng Sấm sét giữa đợt 4–8, quái vẫn ra theo danh sách; không đo nhịp từng giây.
- `feature-hud-upgrade/bar-layout/bar-buttons#no-cover`: ở 16:9 thanh nằm y 920–1060, mặt đất trên y 900.
- `feature-popup-lose/lose-actions/actions-spec#no-other-close`: phần chạm nền che và chạm ngoài khung đạt; phím Escape không giả lập được qua unity-mcp, chưa kiểm.
- `feature-run/run-start/start-values#start-castle-1000`
- `feature-run/run-start/start-values#start-money-50`
- `feature-run/run-start/start-values#start-levels-1`
- `feature-run/run-start/start-values#start-skills-ready`
- `feature-run/run-start/start-values#edge-choi-lai-ngay-sau-thua`
- `feature-run/run-start/replay-checks#params`: 3 lượt liền ở IDEA.
- `feature-run/run-start/replay-checks#replay-no-growth`
- `feature-run/run-start/replay-checks#popup-cleared-on-reset`
- `feature-run/run-start/replay-checks#edge-tien-nang-cap-dot-mau-thanh-cua-luot`: lượt 1 kết thúc ở đợt 9, 872 xu, nhiều cấp, máu tối đa 1150; lượt 2 về 50, cấp 1, đợt 1, 1000/1000.
- `feature-run/run-start/replay-checks#only-record-saved`: kỉ lục 8 còn trong PlayerPrefs sau khi Stop Play; trong game các lượt sau vẫn hiện kỉ lục 8.

Chưa kiểm (vướng công cụ hoặc không gặp tình huống):
- `feature-run/run-start/replay-checks#edge-luot-ket-thuc-luc-dang-co-hieu-ung`: chưa thua đúng lúc hiệu ứng đang chạy; chỉ thấy hiệu ứng lượt cũ đã bị huỷ ở đầu lượt sau.
- `feature-run/run-start/replay-checks#edge-tat-game-giua-tran`: chỉ Stop ở popup thua và lúc chuyển scene, chưa Stop giữa trận.
- `feature-run/run-start/start-values#edge-tien-thuong-dot-hay-tien-ha-quai-toi`: chưa thua đúng lúc đợt vừa dọn.
- `feature-run/run-start/start-values#edge-hieu-ung-ki-nang-con-do-tu-luot`: Băng giá không có ở IDEA.
- `feature-wave/spawning/spawn-order#edge-hai-con-ra-cung-cho`: thấy 3 Lính cùng X 7.2 và cùng X −5.7 khi đang đánh, nhưng không theo được lúc tách.
- `feature-wave/wave-gap/gap-timer#gap-same-rules`: chưa canh đúng 3 giây thở.
- `feature-castle/castle-attack/auto-shot-values#edge-phat-ban-lam-quai-ve-0-mau`: tiền tăng khi quái bị hạ, nhưng không tách riêng được phát bắn của thành.
- `feature-hero/hero-list/hero-roster-values#hero-feedback`: nhún, nháy đỏ, vệt chém, đạn bay không bắt được khung qua unity-mcp.
- `feature-popup/popup-layer/stack-rules#edge-nguoi-choi-roi-game-luc-co-popup`
- `feature-scene-flow/scene-list/boot-loading#edge-nguoi-choi-roi-game-ve-man-hinh-chinh`
- `feature-scene-flow/scene-request/switch-rules#edge-nguoi-choi-roi-game-luc-dang-chuyen`
- `feature-session-scene/session-steps/participant-order#edge-nguoi-choi-roi-game-luc-dang-tai`
- Bốn mục bỏ focus lúc tải vừa nêu chưa kiểm vì Loading xong trước khi lệnh unity-mcp đầu tiên tới, và unity-mcp không bỏ focus cửa sổ được.

Không áp dụng ở IDEA:
- `feature-hero/hero-targeting/targeting-values#edge-muc-tieu-bi-bang-gia-lam-cham`: chưa có Băng giá.
- `feature-castle/castle-skills/skill-values#edge-quai-bi-cham-roi-bi-ha`: ghi rõ là PROTOTYPE.
- `feature-enemy/enemy-stats/enemy-stat-values#edge-tam-dung`: IDEA chưa có tạm dừng.

## Kết quả
đạt: cả 9 bước chạy đúng thiết kế IDEA, chơi 4 lượt liền không dồn object và console không lỗi; có 1 lỗi bố cục nhẹ ở 5:3 và vài mục chỉ kiểm được một phần vì giới hạn của unity-mcp.

## Lỗi tìm thấy
- Lỗi 1, nhẹ, HudUpgrade:
  - Thấy ở bước 3, ảnh `05-battle-5x3.png`, Game view 800x480 (tỉ lệ 5:3).
  - Thanh nâng cấp co để kết thúc đúng mép trái cụm kĩ năng: nút 6 x 493–578, Sấm sét x 578–640. Hai bên chạm nhau, khoảng hở 40 px của frame `bar-buttons/main.frame` mất hẳn.
  - Ở 16:9 khoảng hở là 60 px. Không chồng nên không trái `upgrade-gets-rest`, nhưng nhìn dính vào nhau.
  - Cách tính trong `UpgradeBarView.LayoutSettings` dùng `referenceResolution.x - skillsLeft` làm phần bên phải, không trừ thêm khoảng hở.
- Ghi chú cho Hud (không chắc là lỗi, nhờ @CodeLead hoặc người quyết):
  - Nút cheat tím "Cheat: mid-battle" luôn hiện ở bản dev, tại x 40–300, y 150–200, tức nằm trên dải chiến trường (y 140–900). Ảnh 02, 04, 10.
  - Nếu ý `strip-never-covered` áp cả cho bản dev thì đây là chỗ che; ở bản phát hành nút không có.

## Cấu trúc thư mục
- README.md: file này
- images/01-loading.png: màn Loading (tên game, thanh tiến độ), chụp nhầm ở Game view dọc 720x1280
- images/02-battle-start-16x9.png: trận lượt 1 ở 16:9 (đợt 2): nền, HUD, thanh nâng cấp 6 nút, cụm Sấm sét, không có nút tạm dừng
- images/03-after-buy.png: trận sau khi mua nâng cấp hero 1 và máu thành
- images/04-thunder.png: Sấm sét vừa dùng (không có quái trong vùng), nút hiện hồi chiêu 20
- images/05-battle-5x3.png: trận ở 800x480 (5:3): thanh nâng cấp chạm nút Sấm sét (lỗi 1)
- images/06-thunder-hit.png: ngay sau Sấm sét trúng 4 quái gần thành, hồi chiêu 20
- images/07-lose-popup.png: popup thua lượt 1: điểm 8, kỉ lục 8, "Kỉ lục mới!", chỉ nút Chơi lại
- images/08-run2-start.png: đầu lượt 2 sau Chơi lại
- images/09-run2-lose-popup.png: popup thua lượt 2: điểm 1, kỉ lục 8, không có dòng kỉ lục mới
- images/10-run4-start.png: đầu lượt 4: 50 xu, 1000/1000, Đợt 1, mọi nút Cấp 1 (nút Kĩ năng 60 mờ)
- images/11-loading-16x9.png: định chụp Loading ở 16:9 nhưng lệnh tới trễ, ảnh đã là trận đầu lượt mới
