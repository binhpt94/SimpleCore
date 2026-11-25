# IDEA dot cuoi Home Esc B1 SimClock mot luot tron
Tester: UnityAutoPlay · Đợt: 20261125-235641

## Mục đích
- @Manager giao qua hộp thư (tin `m20261125-235627-5321b7`, 2026-11-25 23:56): đợt chơi thử cuối của IDEA.
- Trước đợt: CodeLead đã nhả Editor, EditMode 1388/1388 qua. Mới từ đợt trước: mọi logic gameplay chạy theo nhịp chung SimClock 40 bước mỗi giây, ngẫu nhiên theo seed, bản dev in seed ra console và có tua nhanh.
- Năm mục cần kiểm:
  1. Home `home-screen-spec#edge-nut-quay-lai-cua-may`: ở màn bắt đầu bấm Esc, màn đứng yên.
  2. Balance `break-forecast#skill-gap`: ít nhất 5 lượt đội mẫu, ghi đợt gãy từng lượt; mong đợi chơi vụng gãy quanh đợt 10, chơi khéo tới đợt 20-25; bật tua nhanh SimClock.
  3. B1 còn dở của đợt `20261125-184253`, chạy lại với tua nhanh (gộp với mục 2).
  4. Kiểm nhanh SimClock: seed in mỗi lượt; cùng seed thì thả quái cùng thứ tự; tạm dừng và hồi sinh không làm nhịp chạy tiếp; mua nâng cấp áp ở bước sau.
  5. Chơi một lượt trọn vẹn, console không lỗi.
- Không kiểm: tạm dừng qua RunConfig và Băng giá (đã dời PROTOTYPE).

## Test gì
- Scene `Assets/_Composition/Loading.unity`, Play, game tự sang `Game.unity`. Không bật state cheat nào.
- Feature và node:
  - Home: `live/feature-home/home-screen/home-screen-spec` (trường hợp biên "Nút quay lại của máy: không làm gì").
  - Balance: `live/feature-balance/balance-difficulty-curve/break-forecast#skill-gap`.
  - SimClock: `live/feature-sim-clock` (`clock-step/clock-values`, `seeded-random/random-values`).

## Môi trường
- Commit: guard chặn lệnh git trong phiên tester; HEAD đầu phiên là `2d8bf95` cộng các thay đổi của các phiên trong ngày.
- Unity 6000.3.10f1, instance `UnityProject@1ec6164a`, Play trong Editor (bản dev). Editor được focus, khoảng 200-400 khung/giây.
- SimClock: tick 40 bước/giây, chế độ FixedTick, seed bản dev 12345. Tua nhanh bằng `SimClockService.SetSpeed(16)` (hàm dev, chỉ trong bộ nhớ lúc Play), đạt khoảng 14 lần giờ thật.
- Cách đo: `execute_code` gắn hàm vào `Application.onBeforeRender` và event `RunFlow.StepChanged`, `WaveDirector.WaveStarted`; đọc `SimClockService`, `EnemyField._alive`, `UpgradeShopLogic`; không ghi file hay asset.
- Bấm nút như người chơi bằng `ExecuteEvents.pointerClickHandler` (nút nâng cấp, nút Sấm sét, nút Chơi lại). Không ép thua lượt nào.

## Các bước
Viết tắt: MĐ là mong đợi theo sổ hoặc thiết kế, TT là thực tế thấy.

1. **Home: bấm Esc ở màn bắt đầu.**
   - Play từ Loading: game vào thẳng scene Game, không có màn bắt đầu.
   - `Boot.Game.prefab` để `HomeInstaller` trong `fromPrototype`: Boot bỏ qua nó ở IDEA (cùng với TeamSelect, GameFlow, Ads, PopupRevive, PopupPause; `BootTests` cũng liệt kê như vậy).
   - Không có màn để bấm Esc.
   - Kết quả: **không áp dụng ở IDEA, không kiểm được**. Kiểm khi Boot bật Home ở PROTOTYPE.
2. **SimClock: mua nâng cấp áp ở bước sau** (dữ liệu 02).
   - Đầu lượt 1, tốc độ 1. `Freeze(true)` đồng hồ (hàm dev), bấm nút nâng Hiệp sĩ (30 xu).
   - MĐ: thao tác vào hàng chờ, áp ở đầu bước kế tiếp.
   - TT:
     - lúc bấm và nhiều khung sau, bước vẫn 1287, Hiệp sĩ vẫn cấp 1;
     - `StepOnce()`: bước 1288, Hiệp sĩ cấp 2, giá kế 41;
     - `StepOnce()` lần nữa: không mua thêm.
   - Kết quả: **đạt**.
3. **SimClock: tạm dừng và hồi sinh không làm nhịp chạy tiếp.**
   - Ở IDEA người chơi không tạm dừng được: `RunFlow.PauseEnabled=False`, `RunSession.pauseOnLeave=False`, `backKeyPauses=False`.
   - Không có hồi sinh thành: `ReviveOnce=False`, PopupRevive tắt.
   - Không có thao tác người chơi nào dựng được hai trạng thái này. Theo lệnh, mục tạm dừng qua RunConfig đã dời PROTOTYPE.
   - Kết quả: **không áp dụng ở IDEA, không kiểm được** bằng chơi thử. Phần này có test EditMode `RunSimClockTests`.
4. **B1: 5 lượt trở lên, tua nhanh x16** (dữ liệu 02).
   - Bot chỉ bấm nút như người chơi, đội mặc định (Hiệp sĩ, Đấu sĩ rìu, Cung thủ). Ở IDEA chỉ có Sấm sét, chưa có Băng giá, Cổ vũ hay chọn đội.
   - MĐ theo `skill-gap`: chơi vụng gãy khoảng đợt 10, chơi khéo tới khoảng đợt 20-25.
   - TT (đợt gãy là đợt thành vỡ; điểm bằng số đợt đã dọn):

     | Lượt | Cách chơi | Đợt gãy | Điểm | Giờ trận | Giờ thật (x16) | Cấp lúc thua (3 hero / máu, sức đánh thành / kĩ năng) |
     |---|---|---|---|---|---|---|
     | 1 | vụng-A: mua chậm (8 giây một lần), bấm sét ngay khi hồi xong; 4 đợt đầu không mua | 12 | 11 | khoảng 5.4 phút | 17 s từ đợt 5 | 6/6/6 · 5/5 · 4 |
     | 2 | vụng-B: chỉ nâng hero, 8 giây một lần, sét ngay khi hồi | 11 | 10 | 4.5 phút | 18 s | 7/7/8 · 1/1 · 1 |
     | 3 | thường: mua xoay vòng liên tục, sét khi từ 3 quái dồn | 12 | 11 | 5.1 phút | 21 s | 6/7/6 · 5/5 · 3 |
     | 4 | khéo-A: mua món rẻ nhất liên tục (rải đều), sét khi từ 5 quái dồn | 13 | 12 | 5.8 phút | 25 s | 7/7/7 · 5/5 · 4 |
     | 5 | khéo-B: như khéo-A, sét khi từ 4 quái | 13 | 12 | 5.8 phút | 24 s | 7/7/7 · 5/5 · 4 |
     | 6 | lặp đúng khéo-A | 13 | 12 | 5.8 phút | 24 s | 7/7/7 · 5/5 · 4 |
     | 7 | khéo-A tới khoảng đợt 5, sau đó không thao tác | 10 (Trùm) | 9 | 4.0 phút | 16 s | 4/4/4 · 3/2 · 1 |

   - Nhận xét:
     - chơi vụng hay bỏ dở gãy ở đợt 10-12, khớp "vụng khoảng đợt 10";
     - cách chơi tốt nhất bot làm được chỉ tới đợt 13, xa mức "khéo tới 20-25";
     - khoảng cách vụng và khéo chỉ 1-3 đợt, nhỏ hơn mong đợi khoảng 10 đợt;
     - lượt dài 4-6 phút giờ trận, ngắn hơn dự đoán 7-10.5 phút.
   - Giới hạn:
     - bot không phải người chơi giỏi (không chọn đội hợp quái, mua theo luật đơn giản);
     - IDEA thiếu Băng giá và chọn đội, mà thiết kế nêu cả hai là chỗ khác nhau giữa vụng và khéo;
     - seed dev cố định nên mỗi cách chơi chỉ cho một kết quả.
   - Kết quả: **không đạt phần "khéo tới đợt 20-25"** (tối đa đợt 13); phần "vụng khoảng đợt 10" đạt. Bảng giai đoạn của `break-forecast` ghi IDEA "không bắt buộc" kiểm bằng chơi thử, nên đây là số liệu cho Balance chỉnh số, không phải lỗi code.
5. **B1 của đợt `20261125-184253`.** Gộp vào bước 4 (cùng yêu cầu `skill-gap`, đã chạy với tua nhanh). Kết quả: như bước 4.
6. **SimClock: seed mỗi lượt và cùng seed thì thả quái cùng thứ tự** (dữ liệu 02).
   - Console có đúng 7 dòng `[SimClock] Seed của lượt (bản dev, từ config): 12345`, mỗi lượt một dòng.
   - Ghi 60 con quái đầu mỗi lượt (loại, bên ra, theo `SpawnOrder`): lượt 2-7 giống hệt nhau từng con, dù cách mua và bấm sét khác nhau. Lượt 1 bắt đầu ghi giữa chừng nên không so.
   - Thêm: lượt 6 lặp đúng cách chơi lượt 4 và cho kết quả y hệt (máu thành ở đợt 10 là 650.8998 cả hai lượt, gãy đợt 13, 350.3 giây gameplay, cùng cấp nâng cấp).
   - Kết quả: **đạt**.
7. **Một lượt trọn vẹn, console** (ảnh 01).
   - Bảy lượt chạy từ đợt 1 tới khi thành vỡ (lượt 1 từ lúc vào trận), bấm Chơi lại 6 lần.
   - Đọc console hai lần (giữa đợt và trước Stop Play): chỉ có log seed, không có dòng Error hay Warning nào. Cảnh báo "Hud cheat 'mid-battle': not applied" của các đợt trước không còn.
   - Kết quả: **đạt**.

## Kết quả
dở: SimClock (seed, thả quái cùng thứ tự, mua áp ở bước sau) và lượt trọn vẹn không lỗi console đều đạt; Home Esc và tạm dừng hay hồi sinh không áp dụng ở IDEA; B1 không đạt phần chơi khéo (tối đa đợt 13 so với mong đợi 20-25).
- Đạt: SimClock seed in mỗi lượt và cùng seed thả quái cùng thứ tự; mua nâng cấp áp ở bước sau; một lượt trọn vẹn không lỗi console.
- Không áp dụng ở IDEA: Home `feature-home/home-screen/home-screen-spec#edge-nut-quay-lai-cua-may` (không có màn bắt đầu); tạm dừng và hồi sinh thành.
- Không đạt (số liệu cân bằng, không phải lỗi code): B1 `feature-balance/balance-difficulty-curve/break-forecast#skill-gap`. Vụng gãy đợt 10-12 đúng dự đoán; khéo chỉ tới đợt 13.

## Lỗi tìm thấy
1. **B1, chơi khéo không kéo được tới đợt 20-25** (bước 4; dữ liệu 02; feature Balance, số liệu ở Wave, Enemy, Upgrade, Castle, Hero).
   - Tái hiện: Play từ Loading, seed dev 12345, đội mặc định. Mua món rẻ nhất mỗi lần có tiền và bấm Sấm sét khi từ 5 quái dồn: thành vỡ ở đợt 13 (khoảng 350 giây trận). Năm cách chơi khác gãy ở đợt 10-13.
   - Theo `skill-gap` cần khoảng cách vụng và khéo đủ lớn (đợt 10 so với 20-25). Thực tế chênh 1-3 đợt.
   - Một phần do IDEA chưa có Băng giá và chọn đội. Cần Balance xem lại khi có PROTOTYPE, hoặc chỉnh mục tiêu ở IDEA.
2. Không phải lỗi game, ghi để biết: nút tím "Cheat: mid-battle" (`HudView/Safe/Cheat`) vẫn hiện trên HUD khi chơi thường ở bản dev (feature Hud; ảnh 01, góc trên). Đã ghi ở các đợt trước.

## Cấu trúc thư mục
- README.md: file này
- images/01-run7-lose-popup-wave10.png: popup thua của lượt 7 (đợt 10, "Trụ được 9 đợt", "Kỉ lục: 12 đợt"), góc trên có nút Cheat mid-battle của HUD
- images/02-b1-runs-simclock-log.txt: nhật kí 7 lượt B1 (cách chơi, đợt gãy, giờ, cấp), thứ tự 60 con quái đầu mỗi lượt, dòng seed trên console, phép thử Freeze và StepOnce khi mua nâng cấp, cờ tạm dừng và hồi sinh ở IDEA
