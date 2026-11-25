# PROTOTYPE 1 tam dung Bang gia hoi sinh B1
Tester: UnityAutoPlay · Đợt: 20261126-003105

## Mục đích
- @Manager giao qua hộp thư (tin `m20261126-003040-30273d`, 2026-11-26 00:30): đợt chơi thử PROTOTYPE 1.
- Trước đợt: CodeLead đã verify mọi yêu cầu kiểm bằng test, EditMode 1591/1591 qua. Boot ở PROTOTYPE: giao chiến qua Combat; tạm dừng, hồi sinh, Băng giá, Cổ vũ đã bật. Chưa kiểm luồng màn bắt đầu và chọn đội vì GameFlow chưa định tuyến.
- Chín mục cần kiểm:
  1. Run `pause-rules#pause-freeze-all`
  2. Run `pause-rules#pause-block-input`
  3. Run `pause-rules#edge-tam-dung-giua-luc-nghi-3-giay`
  4. Run `start-values#edge-hieu-ung-ki-nang-con-do-tu-luot`
  5. Castle `skill-values#edge-quai-bi-cham-roi-bi-ha`
  6. Enemy `enemy-stat-values#edge-tam-dung`
  7. PopupRevive `revive-spec#back-key-nothing`
  8. Balance `break-forecast#skill-gap` (ít nhất 5 lượt, tua nhanh, so với đợt IDEA)
  9. Một lượt trọn vẹn có hồi sinh qua quảng cáo giả, console sạch
- Đợt bị ngắt giữa chừng, xem mục Kết quả.

## Test gì
- Scene `Assets/_Composition/Loading.unity`, Play, game sang `Game.unity`. Không bật state cheat nào.
- Node:
  - `live/feature-run/run-pause/pause-rules`;
  - `live/feature-run/run-start/start-values`;
  - `live/feature-castle/castle-skills/skill-values`;
  - `live/feature-enemy/enemy-stats/enemy-stat-values`;
  - `live/feature-popup-revive/revive-popup/revive-spec`;
  - `live/feature-balance/balance-difficulty-curve/break-forecast`.

## Môi trường
- Commit: guard chặn lệnh git trong phiên tester. Lúc đợt chạy, các phiên GameFlow, TeamSelect, Home đang ghi `.cs` song song (theo Manager).
- Unity 6000.3.10f1, instance `UnityProject@1ec6164a`, Play trong Editor (bản dev). SimClock 40 bước/giây, seed dev 12345. Tua nhanh bằng `SetSpeed` (hàm dev), chỉ để đi nhanh tới lúc cần kiểm; mọi phép kiểm chạy ở tốc độ 1.
- Vào trận:
  - màn bắt đầu "Thủ thành anh hùng / Kỉ lục: 12 đợt / Chơi" hiện, bấm "Chơi" thì Run sang bước TeamSelect;
  - màn trống, không có màn chọn đội (ảnh 00), vì GameFlow chưa định tuyến nên không ai gọi `TeamSelectScreen.Show()`;
  - để vào trận, tester gọi `RunFlow.EnterBattle(DefaultTeam)` (Hiệp sĩ, Đấu sĩ rìu, Cung thủ), đúng lệnh GameFlow sẽ gọi. Đây là đường vòng thay GameFlow, không phải thao tác người chơi.
- Cách đo: `execute_code` đọc `SessionSceneLogic.participants` (RunFlow, SimClockService, WaveDirector, CastleSkills, EnemyField, HeroSquad, CombatLoop) và vị trí, scale các hình. Hook trên `Application.onBeforeRender` tự bấm nút Tạm dừng đúng lúc cần. Bấm nút bằng `ExecuteEvents.pointerClickHandler`. Không ghi file hay asset.

## Các bước
Viết tắt: MĐ là mong đợi theo thiết kế, TT là thực tế thấy. Số đo đủ ở dữ liệu 03.

1. **Mục 1, `pause-freeze-all`** (ảnh 01, dữ liệu 03).
   - Đợt 5: bấm Sấm sét, Băng giá, Cổ vũ rồi bấm nút Tạm dừng trên HUD.
   - Lúc vào Paused:
     - hồi chiêu 20/35/25 giây;
     - hai quái đang chậm 5 giây;
     - hai hiệu ứng sét đang hiện;
     - mũi tên Cung thủ đang bay;
     - đếm thả quái −11.85.
   - MĐ: quái, hero, đạn, hồi chiêu, nghỉ giữa đợt đứng yên; tiếp tục thì chạy từ đúng giá trị cũ.
   - TT:
     - sau khoảng 2000 khung (8 giây), mọi giá trị và mọi vị trí, scale hình giống hệt; bước gameplay giữ 4151, chỉ bước nền của đồng hồ tăng;
     - bấm "Tiếp tục": bước kế tiếp hồi chiêu 19.975/34.975/24.975, chậm 4.975, đếm thả quái −11.875, hero và đạn đi tiếp từ chỗ cũ.
   - Chưa thấy: hồi sinh hero đứng yên khi tạm dừng (lúc tạm dừng không hero nào đang gục). Hook bấm tạm dừng lúc hero đang chờ hồi sinh chưa kịp chạy thì bị ngắt.
   - Kết quả: **đạt**, thiếu phần hero đang chờ hồi sinh.
2. **Mục 2, `pause-block-input`** (ảnh 01, dữ liệu 03).
   - Đang Paused, bấm thẳng (lọt qua popup) cả 6 nút nâng cấp và 3 nút kĩ năng.
   - MĐ: không mua, không dùng kĩ năng.
   - TT:
     - tiền giữ 469, cấp giữ 1/1/1/1/1/1, hồi chiêu giữ 20/35/25 sau khoảng 900 bước nền;
     - thanh nâng cấp mờ khi tạm dừng, đúng `bar-buttons`.
   - Kết quả: **đạt**.
3. **Mục 3, `edge-tam-dung-giua-luc-nghi-3-giay`** (dữ liệu 03).
   - Hook bấm Tạm dừng giữa lúc nghỉ trước đợt 7.
   - TT:
     - còn 1.900 giây, đứng yên suốt khoảng 9 giây tạm dừng;
     - bấm Tiếp tục: đợt 7 bắt đầu sau đúng 76 bước gameplay (1.900 giây).
   - Kết quả: **đạt**.
4. **Mục 6, `enemy-stat-values#edge-tam-dung`** (ảnh 02, dữ liệu 03).
   - Hook bấm Tạm dừng khi hình Sói #85 bắt đầu thu nhỏ.
   - TT lúc tạm dừng: scale đứng yên ở 0.2159 suốt khoảng 1350 khung (phần "đứng yên tới khi tiếp tục" đạt).
   - Sau khi bấm Tiếp tục bằng một lệnh `execute_code`, hình mất ngay ở khung đầu. Nguyên nhân có thể do cách đo: hình thu nhỏ theo `Time.deltaTime` trong 0.3 giây, và khung ngay sau lệnh `execute_code` thường dài khoảng 0.3 giây vì biên dịch code trên main thread.
   - Đang làm lại bằng hook tự bấm Tiếp tục (ghi cả `deltaTime`) thì Unity nạp lại code.
   - Kết quả: **dở** (phần tạm dừng đạt, phần chạy tiếp sau khi tiếp tục chưa kết luận).
5. **Bị ngắt** (khoảng 00:36).
   - Unity biên dịch lại và nạp lại domain giữa lúc Play (editor state: có biên dịch mới, `external_changes_dirty`), mất hết bộ đo.
   - Tester Stop Play và nhắn @Manager tin BÁO CHẶN.
   - @Manager trả lời: các phiên GameFlow, TeamSelect, Home đang ghi `.cs` song song; chốt đợt ở trạng thái dở.
   - Mục 4, 5, 7, 8, 9 chưa chơi.

## Kết quả
dở: mục 1, 2, 3 đạt (mục 1 thiếu phần hero đang chờ hồi sinh), mục 6 dở, mục 4, 5, 7, 8, 9 chưa chơi vì Unity nạp lại code giữa lúc Play do đợt code PROTOTYPE chạy song song.
- Đạt (gửi @CodeLead):
  - `feature-run/run-pause/pause-rules#pause-freeze-all` (trừ phần hồi sinh hero, chưa thấy);
  - `feature-run/run-pause/pause-rules#pause-block-input`;
  - `feature-run/run-pause/pause-rules#edge-tam-dung-giua-luc-nghi-3-giay`.
- Dở: `feature-enemy/enemy-stats/enemy-stat-values#edge-tam-dung`.
- Chưa chơi:
  - `feature-run/run-start/start-values#edge-hieu-ung-ki-nang-con-do-tu-luot`;
  - `feature-castle/castle-skills/skill-values#edge-quai-bi-cham-roi-bi-ha`;
  - `feature-popup-revive/revive-popup/revive-spec#back-key-nothing`;
  - `feature-balance/balance-difficulty-curve/break-forecast#skill-gap`;
  - một lượt trọn vẹn có hồi sinh qua quảng cáo giả.
- Làm tiếp ở đợt mới (Manager giao khi đợt code xong, CodeLead báo biên dịch sạch):
  - mục 1 phần hero đang chờ hồi sinh;
  - mục 6 bằng hook tự bấm Tiếp tục;
  - mục 4, 5, 7, 8, 9;
  - luồng màn bắt đầu, chọn đội, trận.
- Ghi chú cho đợt sau: Esc được đọc bằng `Input.GetKeyDown(KeyCode.Escape)` trong các view (`RunAppSignalsView`, `PausePopupView`), tester không gửi phím thật được. Định giả lập phím bằng cách phát event `BackPressed` của mọi view đang nghe.

## Lỗi tìm thấy
không có lỗi game mới trong phần đã chơi.
- Đã biết, không tính lỗi (Manager báo trước): bấm "Chơi" ở màn bắt đầu thì sang bước TeamSelect nhưng màn trống, không có màn chọn đội, kẹt ở đó (ảnh 00; feature GameFlow chưa định tuyến). Người chơi thật không vào được trận ở bản hiện tại.
- Không phải lỗi game: Unity nạp lại code giữa lúc Play, do các phiên code ghi `.cs` song song.
- Còn tồn từ các đợt trước: nút tím "Cheat: mid-battle" hiện trên HUD khi chơi thường ở bản dev (Hud; ảnh 01, góc trên).

## Cấu trúc thư mục
- README.md: file này
- images/00-teamselect-blank.png: màn trống sau khi bấm "Chơi" (Run ở bước TeamSelect, không có màn chọn đội)
- images/01-paused-skills-cooldown.png: popup Tạm dừng ở đợt 5, ba kĩ năng đang hồi 20/35/25, thanh nâng cấp mờ
- images/02-paused-dying-wolf.png: tạm dừng ngay sau khi hạ Sói #85, hình đang thu nhỏ đứng yên (mục 6)
- images/03-pause-log.txt: số đo mục 1, 2, 3, 6: ảnh chụp trạng thái trước, trong, sau tạm dừng, đếm bước lúc nghỉ, scale hình đang thu nhỏ, lúc bị ngắt
