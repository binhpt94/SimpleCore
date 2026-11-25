# IDEA lam tiep R1 W2 popup thua B1
Tester: UnityAutoPlay · Đợt: 20261125-205040

## Mục đích
- @Manager giao qua hộp thư (tin `m20261125-205007-ef44ee`, 2026-11-25), mức IDEA.
- Làm tiếp phần dở của đợt `20261125-184253` (dừng lúc 19:03 vì người tắt máy):
  1. R1 `feature-run/run-start/replay-checks#edge-luot-ket-thuc-luc-dang-co-hieu-ung`.
  2. W2, phần hồi sinh của `feature-wave/wave-gap/gap-timer#gap-same-rules`: hero gục hồi sinh sau 8 giây trong lúc thở.
  3. Kiểm lại popup thua ở IDEA không còn nút "Về màn bắt đầu", sau bản sửa của PopupLose.
  4. Để cuối: B1 `feature-balance/balance-difficulty-curve/break-forecast#skill-gap`, 5 lượt đội mặc định không ép thua; ghi đợt gãy và thời gian thật mỗi lượt; 2 lượt đầu mỗi lượt quá 30 phút thật thì dừng, ghi dở.
- Bỏ qua theo lệnh: R3, C2, H2, E1, tạm dừng, Home, PopupRevive, TeamSelect, GameFlow, Ads, PopupPause.
- Danh sách đạt gửi @CodeLead để `verify`.

## Test gì
Scene `Assets/_Composition/Loading.unity`, Play, game tự sang `Game.unity`. Không dùng state cheat của feature nào.

| # | Feature | Yêu cầu | Cách thấy khi Play (theo sổ) |
|---|---|---|---|
| R1 | Run | `feature-run/run-start/replay-checks#edge-luot-ket-thuc-luc-dang-co-hieu-ung` | Thua lúc kĩ năng thành đang chạy hoặc quái chết dở, chơi lại: hiệu ứng cũ không chạy sang lượt mới |
| W2 | Wave | `feature-wave/wave-gap/gap-timer#gap-same-rules` (phần hồi sinh) | Trong 3 giây thở hero gục vẫn hồi sinh (8 giây) |
| L2 | PopupLose | `feature-popup-lose/lose-actions/actions-spec` (bảng giai đoạn: nút Về màn bắt đầu IDEA "Không có") | Popup thua ở IDEA chỉ có nút "Chơi lại" |
| B1 | Balance | `feature-balance/balance-difficulty-curve/break-forecast#skill-gap` | Chơi ít nhất 5 lượt đội mẫu, ghi đợt gãy (vụng khoảng đợt 10, khéo khoảng đợt 20-25) |

## Môi trường
- Commit: guard chặn lệnh git trong phiên tester; HEAD theo gitStatus đầu phiên là `2d8bf95` (cộng thay đổi chưa commit của các phiên khác nếu có).
- Unity 6000.3.10f1, instance `UnityProject@1ec6164a` (máy vừa khởi động lại, hash mới; guard nêu đúng id này). Play trong Editor (bản dev).
- Editor đang được focus: khoảng 200-400 khung/giây, `Time.time` chạy gần bằng giờ thật.
- Đội mặc định của lượt: Hiệp sĩ (slot 0), Đấu sĩ rìu (slot 1), Cung thủ (slot 2). Kỉ lục đầu đợt trong PlayerPrefs là 7 (popup ghi "Kỉ lục: 7 đợt"), không phải 9 như tin giao việc ghi.
- Cách đo: `execute_code` gắn hàm vào `Application.onBeforeRender` và các event (`CastleSkills.ThunderStruck`, `RunFlow.StepChanged`, `CastleBody.Destroyed`, `HeroSquad.Fell/Respawned`, `WaveDirector.WaveCleared/WaveStarted`); chỉ trong bộ nhớ, không ghi file hay asset. Đọc trạng thái qua `SessionSceneLogic.participants`.
- Bấm nút như người chơi bằng `ExecuteEvents.pointerClickHandler` (nút Sấm sét, nút nâng cấp, nút Chơi lại).
- Ép thành 1 máu chỉ ở R1, bằng `CastleBody.ForceHealth(1)` (cùng đường với cheat của Castle). B1 không ép.

## Các bước
Viết tắt: MĐ là mong đợi theo sổ hoặc thiết kế, TT là thực tế thấy.

1. **R1: thua lúc đang có hiệu ứng, rồi Chơi lại** (dữ liệu 02).
   - Play từ Loading, lượt đầu chạy tự nhiên tới đợt 6. Bấm Sấm sét khi quái sát thành (|x| = 3.0); cùng khung đó ép thành 1 máu.
   - Sấm sét hạ hết 5 quái đang đánh thành, nên thành không vỡ trong 0.3 giây hiệu ứng sét. Thành vỡ 15 giây sau.
   - Lúc thành vỡ (bước Ending), trên màn còn:
     - 1 phát bắn của thành đang bay (`Shot` dưới `CastleView`);
     - 1 quái đang chết dở: 11 hình quái, 10 quái sống. Hình chết dở này còn tới bước Lost.
   - Bấm "Chơi lại" trên popup thua.
   - MĐ: hiệu ứng cũ không chạy sang lượt mới.
   - TT:
     - ở khung bấm, lượt mới đã vào Playing, máu thành 1000;
     - khung +1: số hình quái bằng số quái sống (1 và 1), không còn hiệu ứng nào của thành, không còn chữ tiền bay;
     - 6 giây sau: 5 hình là 5 quái mới (#95-#99).
   - Giới hạn: không dựng được khoảnh khắc thua đúng trong 0.3 giây sét đang hiện, vì Sấm sét hạ luôn mọi quái đang đánh thành. Thay vào đó dùng hai hiệu ứng khác đang chạy lúc thua (phát bắn đang bay, quái chết dở); sổ chấp nhận "kĩ năng đang chạy **hoặc** quái chết dở".
   - Kết quả: **đạt**.
2. **Popup thua ở IDEA không còn nút "Về màn bắt đầu"** (ảnh 01).
   - Ngay ở lần thua của bước 1, và lại ở mọi lần thua của bước 4.
   - MĐ: bảng giai đoạn của `feature-popup-lose/lose-actions/actions-spec` ghi nút này ở IDEA là "Không có".
   - TT:
     - `PopupLayer/LoseMain(Clone)/Home` có trong prefab nhưng inactive;
     - chỉ nút "Chơi lại" hiện và bấm được;
     - chữ đúng: "Thành đã bị phá", "Trụ được 6 đợt", "Kỉ lục: 7 đợt";
     - code: `LosePopupParticipant` giờ có cờ `_homeButton` (tắt ở IDEA), gọi `ShowHomeButton(false)` và không nối `HomePressed`.
   - Ghi nhận nhỏ, không phải lỗi: nút "Chơi lại" nằm lệch trái, chỗ bên phải bỏ trống. Đúng frame `main.frame` của node (replay x=610 cố định, frame không có bản riêng cho IDEA).
   - Kết quả: **đạt**. Lỗi 1 của đợt `20261125-184253` đã sửa.
3. **W2: hero gục vẫn hồi sinh trong lúc thở** (dữ liệu 03).
   - Chạy tự nhiên 4 lượt (không ép), ghi mọi lần hero gục, hồi sinh, dọn sạch đợt, bắt đầu đợt.
   - MĐ: trong 3 giây thở, đồng hồ hồi sinh 8 giây của hero vẫn chạy.
   - TT, ba lần hero đang gục lúc đợt vừa dọn sạch:
     - Đấu sĩ rìu: còn 3.41 giây lúc dọn đợt 7, còn 0.41 lúc đợt 8 bắt đầu, đứng dậy đúng 8.00 giây sau khi gục;
     - Hiệp sĩ: 4.78 xuống 1.78 qua lúc thở sau đợt 10, đứng dậy sau 8.00 giây;
     - Cung thủ: 5.85 xuống 2.85 qua lúc thở sau đợt 10 của lượt sau, đứng dậy sau 8.01 giây.
   - Lúc thở luôn dài đúng 3.00 giây; mọi lần gục khác cũng hồi sinh đúng 8.00 giây.
   - Giới hạn: không lần nào hero gục rồi đứng dậy trọn trong 3 giây thở (cần còn dưới 3 giây lúc dọn đợt). Nhưng đồng hồ hồi sinh giảm đủ 3 giây suốt lúc thở ở cả ba lần, nên phần "hồi sinh vẫn chạy" đã thấy.
   - Các phần khác của W2 (mua, kĩ năng, hồi chiêu, máu thành không hồi) đã đạt ở đợt `20261125-184253`.
   - Kết quả: **đạt**.
4. **B1: Balance 5 lượt.** @Manager bỏ việc này giữa đợt (21:10): người yêu cầu thêm tick cố định để tua nhanh, B1 chạy sau khi có.
   - Số liệu đã có, chỉ để tham khảo (dữ liệu 03):
     - lượt 0, không mua gì: thua đợt 7 (điểm 6), khoảng 2.8 phút;
     - lượt 1 và 2, mua nâng cấp xoay vòng và bấm Sấm sét khi quái dồn từ 3 con: cả hai gãy ở đợt 12 (điểm 11), 5 phút 31 giây và 5 phút 10 giây thật;
     - lượt 3 dừng ở đợt 6.
   - Kết quả: **để sau**, chờ tick cố định.

## Kết quả
đạt: R1, W2 (phần hồi sinh) và popup thua ở IDEA không còn nút "Về màn bắt đầu" đều đúng thiết kế; B1 để sau theo lệnh của @Manager (chờ tick cố định).
- Đạt (gửi @CodeLead verify):
  - R1 `feature-run/run-start/replay-checks#edge-luot-ket-thuc-luc-dang-co-hieu-ung` (thua lúc có phát bắn đang bay và quái chết dở; không dựng được lúc sét đang hiện)
  - W2 `feature-wave/wave-gap/gap-timer#gap-same-rules` (phần hồi sinh đợt này, các phần còn lại đạt ở đợt `20261125-184253`)
  - Popup thua ở IDEA: bảng giai đoạn của `feature-popup-lose/lose-actions/actions-spec` (nút Về màn bắt đầu không có)
- Để sau: B1 `feature-balance/balance-difficulty-curve/break-forecast#skill-gap`.
- Console: không có lỗi hay cảnh báo nào lúc Stop Play.

## Lỗi tìm thấy
không có lỗi mới.
- Lỗi 1 của đợt `20261125-184253` (nút "Về màn bắt đầu" trên popup thua ở IDEA, PopupLose): đã sửa, xem bước 2.
- Lỗi 2 của đợt đó vẫn còn, ghi lại để biết: nút tím "Cheat: mid-battle" (`HudView/Safe/Cheat`) hiện trên HUD ở mọi lượt chơi thường (feature Hud; ảnh 01, góc trên). `HudInstaller` luôn tạo `HudCheat` ở bản dev, nên `HudSession` luôn hiện nút. Thiết kế node `feature-hud/cheat` không nói nút này phải hiện khi chơi thường. Chỉ có ở bản dev.

## Cấu trúc thư mục
- README.md: file này
- images/01-lose-popup-no-home.png: popup thua ở IDEA chỉ có nút "Chơi lại", "Trụ được 6 đợt", "Kỉ lục: 7 đợt"; góc trên có nút Cheat mid-battle của HUD
- images/02-r1-loss-replay-log.txt: nhật kí R1: bấm Sấm sét, ép 1 máu, lúc thành vỡ (phát bắn đang bay, quái chết dở), các khung sau khi bấm Chơi lại
- images/03-w2-respawn-gap-and-runs-log.txt: nhật kí W2 (đồng hồ hồi sinh qua lúc thở), thời gian và đợt gãy các lượt chạy tự nhiên, chữ trên popup thua
