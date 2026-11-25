# PROTOTYPE 2 luong man hoi sinh tam dung B1
Tester: UnityAutoPlay · Đợt: 20261126-010423

## Mục đích
- @Manager giao qua hộp thư (tin `m20261126-010407-f20e0d`, 2026-11-26 01:04): đợt chơi thử PROTOTYPE 2.
- Trước đợt: CodeLead báo biên dịch sạch, EditMode 1668/1668; Play từ Loading vào màn bắt đầu, console 0 lỗi. Guard chặn mọi phiên ghi vào Assets khi tester giữ Editor.
- Tám mục:
  1. Luồng màn: Chơi, chọn đội, Vào trận, thua, Chơi lại về chọn đội, Về màn bắt đầu, Bỏ lượt từ popup tạm dừng.
  2. Home `home-screen-spec#edge-nut-quay-lai-cua-may`.
  3. Run `pause-rules#pause-freeze-all` phần hero đang gục.
  4. Run `start-values#edge-hieu-ung-ki-nang-con-do-tu-luot`.
  5. Enemy `enemy-stat-values#edge-tam-dung` phần chạy tiếp.
  6. Castle `skill-values#edge-quai-bi-cham-roi-bi-ha`.
  7. PopupRevive `revive-spec#back-key-nothing` và luồng hồi sinh theo từng id của `revive-order/order-values`, cheat `waiting-ad`.
  8. Balance `break-forecast#skill-gap` (ít nhất 5 lượt, tua nhanh).
- Ghi đạt hoặc không đạt theo từng id.

## Test gì
- Scene `Assets/_Composition/Loading.unity`, Play; game vào màn bắt đầu trong scene `Game.unity`. Mọi feature PROTOTYPE bật (Home, TeamSelect, GameFlow, tạm dừng, PopupRevive, Ads giả, Băng giá, Cổ vũ).
- Node:
  - `live/feature-home/home-screen/home-screen-spec`;
  - `live/feature-run/run-pause/pause-rules`;
  - `live/feature-run/run-start/start-values`;
  - `live/feature-enemy/enemy-stats/enemy-stat-values`;
  - `live/feature-castle/castle-skills/skill-values`;
  - `live/feature-popup-revive/revive-popup/revive-spec`;
  - `live/feature-popup-revive/revive-order/order-values`;
  - `live/feature-popup-revive/cheat` (`waiting-ad`);
  - `live/feature-balance/balance-difficulty-curve/break-forecast`.

## Môi trường
- Commit: guard chặn lệnh git trong phiên tester. Lượt này không có lần nạp lại code nào giữa lúc Play.
- Unity 6000.3.10f1, instance `UnityProject@1ec6164a`, Play trong Editor (bản dev). SimClock 40 bước/giây (1 bước = 0.025 giây), seed dev 12345. Đo thời gian bằng số bước SimClock vì giờ thật bị lệnh `execute_code` làm nhiễu.
- Thao tác như người chơi bằng `ExecuteEvents.pointerClickHandler` lên đúng nút (Chơi, Vào trận, Tạm dừng, Tiếp tục, Bỏ lượt, kĩ năng, nâng cấp, Xem quảng cáo, Kết thúc lượt, Chơi lại, Về màn bắt đầu).
- Esc: các view đọc `Input.GetKeyDown(KeyCode.Escape)`. Tester giả lập bằng cách phát event `BackPressed` của mọi MonoBehaviour đang bật có event đó; lúc thử chỉ có `RunAppSignalsView` nghe.
- Đường vòng có ghi rõ, không phải thao tác người chơi:
  - tua nhanh và đứng đồng hồ: `SimClockService.SetSpeed`, `Freeze`, `StepOnce` (hàm dev);
  - cho thành vỡ: `CastleBody.Hit` với toàn bộ máu, cùng đường với `ICastleFallCheat.Fall`;
  - cheat `waiting-ad`: gọi đúng ba hàm cheat mà `ReviveWaitingAdCheat` gọi (`WaveDirector.StartWave(8)`, `FakeAdProvider.HoldNextAd()`, `CastleBody.Hit`), vì cờ `cheatWaitingAd` nằm trong RunConfig asset mà tester không được sửa;
  - "rời game": gửi `OnApplicationFocus(false)` và `OnApplicationPause(true)` tới mọi GameObject, rồi chiều ngược lại.

## Các bước
Viết tắt: MĐ là mong đợi theo thiết kế, TT là thực tế thấy. Số đo đủ ở dữ liệu 05 (mục 1-7) và 06 (mục 8).

1. **Mục 2, Home `edge-nut-quay-lai-cua-may`.**
   - Ở màn bắt đầu giả lập Esc.
   - MĐ: không làm gì.
   - TT: vẫn màn bắt đầu, Run ở bước Home, không chuyển màn.
   - Kết quả: **đạt**.
2. **Mục 1, luồng màn** (ảnh 01, 03).
   - Chơi → màn chọn đội: 5 thẻ hero, 3 ô điền sẵn Hiệp sĩ, Đấu sĩ rìu, Cung thủ, nút Vào trận, Quay lại.
   - Vào trận → trận chạy, 3 hero, console sạch.
   - Thua → popup thua có "Chơi lại" và "Về màn bắt đầu". Chơi lại → về màn chọn đội, trận cũ đã dọn.
   - Ở lượt khác, Về màn bắt đầu → màn bắt đầu ("Kỉ lục: 12 đợt"), trận đã dọn.
   - Ở lượt khác, Tạm dừng → Bỏ lượt → bước xác nhận "Bỏ lượt này? Điểm tính theo số đợt đã dọn." → Bỏ lượt → màn bắt đầu, trận đã dọn.
   - Kết quả: **đạt**.
3. **Mục 3, `pause-freeze-all` phần hero đang gục.**
   - Hook tự bấm Tạm dừng khi hero 1 còn 6.000 giây chờ hồi sinh.
   - TT:
     - suốt 418 bước tạm dừng vẫn 6.000;
     - tiếp tục thì 5.975;
     - đứng dậy sau 241 bước gameplay (tính ra 239; lệch 2 bước do đo ở tốc độ x8).
   - Kết quả: **đạt**.
4. **Mục 5, `enemy-stat-values#edge-tam-dung` phần chạy tiếp.**
   - Hook tự tạm dừng ngay khi Sói #118 bắt đầu thu nhỏ, giữ 300 khung, rồi tự bấm Tiếp tục.
   - TT:
     - lúc tạm dừng, scale đứng ở 0.1842;
     - tiếp tục thì thu nhỏ đều về 0 trong 67 khung (`deltaTime` khoảng 0.0035).
   - Lần trước hình mất ngay ở khung đầu là do khung đó dài vì lệnh `execute_code`, không phải lỗi.
   - Kết quả: **đạt**.
5. **Mục 6, `skill-values#edge-quai-bi-cham-roi-bi-ha`.**
   - Bấm Băng giá; Lính #241 bị hạ khi còn chậm 1.15 giây.
   - TT:
     - trạng thái chậm mất khỏi trận cùng quái;
     - hình giữ màu xanh lúc thu nhỏ rồi biến mất cùng quái;
     - hết chậm: 8 hình, 0 hình bị nhuộm (hình quái mới không mang màu chậm).
   - Kết quả: **đạt**.
6. **Mục 7, luồng hồi sinh** (ảnh 02, 04).
   - `back-key-nothing`: popup hồi sinh mở, giả lập Esc thì popup giữ nguyên, bước vẫn ReviveOffer. **Đạt**.
   - `popup-after-1s`: thành vỡ ở bước 4911, popup mở ở bước 4951, đúng 40 bước = 1.0 giây; trong lúc đó thân thành thu nhỏ về 0 trong khoảng 0.5 giây. **Đạt**.
   - `revive-anim`:
     - thành phóng từ 0.006 lên 0.806 trong 0.498 giây;
     - lúc chạy tiếp máu 300/1000 (30%);
     - chưa đọc được máu đúng ở khung đầu của 0.5 giây.
     - **Đạt** (thiếu phần "máu đặt ngay đầu").
   - `resume-after-anim`: bước Playing tới ngay khi thành hiện xong, không đếm ngược. **Đạt**.
   - `fail-to-lose-now`: bấm "Kết thúc lượt" thì bước kế tiếp đã Lost, popup thua mở ngay. **Đạt**.
   - `offer-flag` (một lần mỗi lượt): sau khi hồi sinh, thành vỡ lần hai thì không có popup hồi sinh, popup thua sau 1.0 giây. **Đạt**.
   - Đồng hồ hero và Băng giá đứng yên khi chờ:
     - hero giữ 0.175 và 1.150 giây suốt ReviveOffer và Reviving;
     - Băng giá còn 4.975 và hồi chiêu 34.975 giữ nguyên suốt ReviveWait và ReviveOffer.
     - **Đạt**.
   - `held-clear` (tin dọn đợt giữ lại tới khi hồi sinh): không dựng được đợt dọn đúng khung thành vỡ. **Không kiểm được.**
   - Tiền quái hạ trước lúc dừng vẫn có: tiền giữ 551 từ lúc vỡ tới popup (không mất, không cộng thêm); không dựng được quái bị hạ cùng khung. **Không kiểm được trọn.**
   - Rời game trong 1 giây trễ:
     - lúc quay lại, popup hồi sinh đang mở, không có popup tạm dừng, trận đứng yên;
     - trong Editor, giả lập rời game không làm Unity ngừng chạy, nên 1 giây trễ vẫn trôi khi "ở ngoài" (popup mở ở khoảng +40 bước).
     - **Đạt phần "quay lại thì popup mở"; phần "trễ không tính lúc ở ngoài" không đo được.**
   - Cheat `waiting-ad`:
     - trận ở đợt 8, thành 0 máu, popup mở;
     - bấm Xem quảng cáo thì hiện "Đang chờ quảng cáo...", hai nút mờ (CanvasGroup alpha 0.4) và khoá;
     - bấm "Kết thúc lượt" lúc đó không có tác dụng.
     - **Đạt** (dựng bằng ba hàm cheat, không qua cờ asset).
7. **Mục 4, `start-values#edge-hieu-ung-ki-nang-con-do-tu-luot`.**
   - Băng giá còn 4.925 giây khi thành vỡ. Kết thúc lượt → Chơi lại → Vào trận.
   - TT: lượt mới không quái nào chậm, 0 hình bị nhuộm, hồi chiêu 0/0/0, thành chỉ còn Body.
   - Kết quả: **đạt**.
8. **Mục 8, B1 `skill-gap`** (dữ liệu 06). Sáu lượt tua x16. Bot bấm nút như người chơi, từ chối hồi sinh để đo đợt gãy.

   | Lượt | Cách chơi | Đợt gãy | Điểm | Giờ trận |
   |---|---|---|---|---|
   | 1 | vụng-A: mua chậm, chỉ Sấm sét | 12 | 11 | 5.6 phút |
   | 2 | vụng-B: chỉ nâng hero, chỉ Sấm sét | 10 | 9 | 4.0 phút |
   | 3 | thường: mua đều, 3 kĩ năng ngưỡng thấp | 14 | 13 | 6.6 phút |
   | 4 | khéo-A: món rẻ nhất, kĩ năng ngưỡng cao | 12 | 11 | 5.5 phút |
   | 5 | khéo-B: món rẻ nhất, ngưỡng vừa | 13 | 12 | 5.9 phút |
   | 6 | khéo-C: món rẻ nhất, Băng giá sớm | 13 | 12 | 6.1 phút |

   - So với IDEA (đợt `20261125-235641`, chỉ có Sấm sét): vụng giữ đợt 10-12; cách chơi tốt nhất lên đợt 14 (IDEA tối đa 13).
   - MĐ: khéo tới đợt 20-25. Không lượt nào vượt đợt 14.
   - Giới hạn: bot không chọn đội khác, không canh Trùm; seed cố định.
   - Kết quả: **không đạt** phần khéo; phần vụng quanh đợt 10 đạt.

## Kết quả
không đạt: mọi mục luồng màn, tạm dừng, Băng giá, hồi sinh kiểm được đều đạt; chỉ B1 `skill-gap` không đạt phần chơi khéo (tối đa đợt 14 so với 20-25).
- Đạt:
  - `feature-home/home-screen/home-screen-spec#edge-nut-quay-lai-cua-may`;
  - luồng màn (mục 1);
  - `feature-run/run-pause/pause-rules#pause-freeze-all` (đủ cả phần hero đang gục);
  - `feature-run/run-start/start-values#edge-hieu-ung-ki-nang-con-do-tu-luot`;
  - `feature-enemy/enemy-stats/enemy-stat-values#edge-tam-dung`;
  - `feature-castle/castle-skills/skill-values#edge-quai-bi-cham-roi-bi-ha`;
  - `feature-popup-revive/revive-popup/revive-spec#back-key-nothing`;
  - `feature-popup-revive/revive-order/order-values`: `popup-after-1s`, `revive-anim` (trừ đọc máu ở khung đầu), `resume-after-anim`, `fail-to-lose-now`, `offer-flag`, trường hợp biên hero và Băng giá đứng yên;
  - `feature-popup-revive/cheat#waiting-ad`.
- Không kiểm được trọn:
  - `order-values#held-clear`;
  - trường hợp biên "tiền hạ quái cùng khung thành vỡ";
  - "rời game trong 1 giây trễ", phần trễ không tính lúc ở ngoài.
- Không đạt: `feature-balance/balance-difficulty-curve/break-forecast#skill-gap`, phần khéo.
- Console: 0 Error, 0 Warning trong cả hai lần Play. Không thấy lỗi biên dịch của Hero.

## Lỗi tìm thấy
1. **B1, chơi khéo không tới đợt 20-25** (bước 8; dữ liệu 06; Balance, số liệu ở Wave, Enemy, Upgrade, Castle, Hero).
   - Tái hiện: Play, Chơi, Vào trận với đội mặc định, seed dev 12345. Mua món rẻ nhất mỗi lần có tiền, dùng cả ba kĩ năng khi quái dồn, từ chối hồi sinh: thành vỡ ở đợt 12-14.
   - Mong đợi theo `skill-gap`: khéo khoảng đợt 20-25.
2. **Nghi vấn, chưa tái hiện được: thân thành dừng ở nửa chừng lúc chờ hồi sinh** (bước 6; Castle hoặc Run).
   - Lần thành vỡ tự nhiên đầu tiên (đợt 8, lượt 1 sau Play), lúc popup hồi sinh đang mở, `CastleView/Body` có scale 0.599 thay vì 0 (thành thường 0.806). Tức là hình thành thu nhỏ được khoảng 25% rồi đứng lại, nằm sau popup.
   - Hai lần vỡ bằng `CastleBody.Hit` sau đó đều thu nhỏ về 0 trong 0.5 giây.
   - Lúc đó tester không ghi được khoảnh khắc vỡ nên không biết bước nào làm hình đứng lại. Cần CodeLead hoặc Castle xem: hình thu nhỏ có dừng theo bước Run (ReviveWait hay ReviveOffer) thay vì chạy hết 0.5 giây không.
3. Còn tồn: nút tím "Cheat: mid-battle" trên HUD khi chơi thường ở bản dev (Hud).

## Cấu trúc thư mục
- README.md: file này
- images/01-teamselect.png: màn chọn đội sau khi bấm Chơi (5 thẻ hero, 3 ô điền sẵn, Vào trận, Quay lại)
- images/02-revive-popup.png: popup hồi sinh lần thành vỡ đầu ở đợt 8 ("Thành đã bị phá", dòng 30% máu, Xem quảng cáo, Kết thúc lượt)
- images/03-lose-popup-two-buttons.png: popup thua ở PROTOTYPE có "Chơi lại" và "Về màn bắt đầu"
- images/04-revive-waiting-ad.png: state cheat waiting-ad, "Đang chờ quảng cáo...", hai nút mờ và khoá
- images/05-flow-pause-revive-log.txt: số đo mục 1-7 (luồng màn, tạm dừng lúc hero gục, quái đang thu nhỏ, quái chậm bị hạ, dòng thời gian hồi sinh, từ chối, rời game, lượt mới sau Băng giá, cheat waiting-ad)
- images/06-b1-runs-log.txt: nhật kí 6 lượt B1 (cách chơi, đợt gãy, điểm, giờ trận, cấp nâng cấp) và so với IDEA
