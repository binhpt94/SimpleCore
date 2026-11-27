# Run: ki luc sau khi chuyen sang feature-save
Tester: UnityAutoPlay · Đợt: 20261127-151417

## Mục đích
@CodeLead nhờ (tin `m20261127-151410-57011f`), mức IDEA: kiểm Run sau khi kỉ lục chuyển sang `feature-save`. Chơi một trận tới khi thành bị phá, xem popup thua hiện điểm và kỉ lục, rồi chơi lại một trận điểm thấp hơn để xem kỉ lục có giữ nguyên.

## Test gì
- Feature: Run (điểm, kỉ lục), Save (ghi và đọc), PopupLose (chữ kết quả, nút), Home (dòng kỉ lục).
- Node `live`: `feature-run/run-score/score-rules`, `feature-run/run-flow/flow-rules`, `feature-popup-lose/lose-result/result-spec`, `feature-popup-lose/lose-actions/actions-spec`.
- Gói thiết kế đã đọc: `pack-Run-e02ec412e749.md`, `pack-PopupLose-2c03dbb403f9.md`.
- Scene: `Assets/_Composition/Loading.unity`, Play.
- Không dùng state cheat chọn sẵn (`CheatSelection.asset` trống).

## Môi trường
- Commit HEAD `3a62c28`, cộng các thay đổi chưa commit (Run bỏ `PlayerPrefsBestScoreStore`, dùng `ISavedRecord`).
- Play trong Editor (bản dev). Cửa sổ Editor không được focus. Seed SimClock từ config: 12345.
- `Boot.prefab` đặt `stage: 1` (PROTOTYPE), nên game chạy theo luồng PROTOTYPE: màn bắt đầu, chọn đội, trận, popup thua có 2 nút. Đợt này chấm phần điểm và kỉ lục; `score-rules` ghi phần này giống nhau ở mọi giai đoạn.
- Trên máy trước đợt: chưa có `~/.config/unity3d/DefaultCompany/UnityProject/record.save`. PlayerPrefs cũ còn `Run/Best` = 9.
- Sau đợt: `record.save` còn trên máy, nội dung `1` / `3` (phiên bản 1, kỉ lục 3). Đây là file game tự ghi khi chơi.
- Thao tác bằng `execute_code` lúc Play:
  - bấm nút qua `pointerClickHandler`;
  - đọc trạng thái bằng reflection;
  - tua trận bằng `SimClockService.SetSpeed(16)` (bản dev). Trước đó có đặt `Time.timeScale` 4 rồi 6; nó không tác dụng lên SimClock và đã trả về 1;
  - cho thua bằng `CastleBody.ForceHealth(1)` sau khi đạt điểm cần, rồi để quái tự đánh vỡ thành;
  - giả lập người chơi quay lại game bằng `SendMessage("OnApplicationFocus", true)` vào `RunAppSignalsView` (ở trận 1).
- Lệnh xoá console bị guard chặn, nên không xoá và không thử lại.

## Các bước
Viết tắt: MĐ là mong đợi theo thiết kế, TT là thực tế thấy.

1. **Mở game, chưa có bản lưu.**
   - MĐ: `feature-save` báo chưa có thì kỉ lục là 0. PlayerPrefs cũ không còn là nơi lưu.
   - TT: màn bắt đầu hiện "Kỉ lục: 0 đợt", dù PlayerPrefs cũ còn 9 (ảnh 01). Đạt.
2. **Vào trận 1** (Chơi, rồi Vào trận với đội Hiệp sĩ, Đấu sĩ rìu, Cung thủ).
   - TT: 50 xu, 1000/1000, Đợt 1, mọi nút Cấp 1 (ảnh 02). Đạt.
3. **Dọn đợt.**
   - MĐ: mỗi đợt dọn sạch cộng 1 điểm.
   - TT: điểm đi 0 → 1 (HUD Đợt 2) → 2 (Đợt 3) → 3 (Đợt 4). Đạt.
4. **Thành bị phá khi đang ở đợt 4.**
   - MĐ: điểm chốt 3 (đợt dở không tính). Kỉ lục thành 3, cờ bật. Ghi xuống máy trước khi popup thua hiện.
   - TT:
     - Lúc `Step=Ending`, popup chưa mở: `Score=3 Best=3 IsNewBest=True IsBestUnsaved=False`.
     - `record.save` đã có, nội dung `1` / `3` (giờ file 15:16:35).
     - Quái, hero đứng yên; thanh nâng cấp mờ.
     - Đạt.
5. **1 giây chờ rồi popup thua.**
   - TT:
     - Vì Editor không focus, lượt coi game đang ở ngoài màn hình (`_held=True`, `elapsed=0`) nên 1 giây đứng yên. Việc này đúng `flow-rules#edge-roi-game-dung-luc-thanh-vua-bi-pha`.
     - Sau khi gửi tín hiệu focus thì popup thua mở. Không đo được thời lượng 1 giây.
     - Đạt.
6. **Popup thua trận 1.**
   - MĐ: "Thành đã bị phá", "Trụ được 3 đợt", "Kỉ lục: 3 đợt", có "Kỉ lục mới!", nút "Chơi lại" và "Về màn bắt đầu".
   - TT: lời mời đánh giá (PopupRating) nằm trên popup thua (ảnh 03). Bấm "Để sau" thì popup thua đúng đủ chữ và nút (ảnh 04). Đạt; có lỗi bố cục nhẹ (lỗi 1).
7. **Chơi lại, vào trận 2.**
   - MĐ: về màn chọn đội (PROTOTYPE) với đội cũ. Đầu lượt điểm 0, kỉ lục giữ 3.
   - TT: đúng. `Score=0 Best=3 IsNewBest=False`, 1000/1000, Đợt 1. Đạt.
8. **Thua trận 2 với điểm 1** (thấp hơn kỉ lục).
   - MĐ: kỉ lục giữ 3, cờ tắt, không có dòng "Kỉ lục mới!" hay dải nền, không ghi đè bản lưu.
   - TT:
     - `Score=1 Best=3 IsNewBest=False IsBestUnsaved=False`.
     - Popup: "Trụ được 1 đợt", "Kỉ lục: 3 đợt", không có dải nền (ảnh 05). Không có lời mời đánh giá.
     - `record.save` vẫn `1` / `3`, giờ file không đổi (15:16:35).
     - Đạt.
9. **Về màn bắt đầu.**
   - TT: màn bắt đầu hiện "Kỉ lục: 3 đợt". Đạt.
10. **Stop Play rồi Play lại** (tắt mở game trong Editor).
    - MĐ: kỉ lục còn, đọc qua `feature-save`.
    - TT: sau Loading, màn bắt đầu hiện "Kỉ lục: 3 đợt" (ảnh 06). Đạt.
11. **Console.**
    - TT:
      - Đọc `~/.config/unity3d/Editor.log` từ lượt Play đầu: không thấy exception hay lỗi của game, không có cảnh báo `[Save] write failed`.
      - Chỉ có 4 dòng lỗi/cảnh báo, các dòng đã xem là cảnh báo Unity về asmdef View chưa có script (GameFlow, Wave, Upgrade, CheatPanel).
      - Console sau Stop Play cuối: 0 lỗi, 0 cảnh báo.

Chưa kiểm:
- `feature-save` ghi thất bại và thử ghi lại (`record-keep-on-fail`, `record-retry`).
- Dữ liệu lưu hỏng hoặc số âm.
- Điểm bằng kỉ lục.
- Dọn sạch đợt cùng khung hình thành bị phá.
- Tắt game ngay trên popup thua (đợt này chỉ Stop ở màn bắt đầu; bản lưu đã có trước khi popup mở, xem bước 4).
- Build trên máy thật.

## Kết quả
đạt: popup thua hiện đúng điểm và kỉ lục; trận 2 điểm 1 giữ kỉ lục 3 mà không ghi đè; kỉ lục còn sau khi Stop rồi Play lại.

## Lỗi tìm thấy
- Lỗi 1, nhẹ, PopupLose (chỉ bố cục, không liên quan kỉ lục):
  - Thấy ở bước 6, ảnh `04-lose1-popup.png`, Game view 16:9.
  - Hai ngôi sao của hình dải nền "Kỉ lục mới!" đè lên chữ: sao trái che mép chữ "K", sao phải che dấu "!".
  - Chưa xem nguyên nhân (cỡ chữ, hay hình dải nền).

Ghi chú, chưa xếp là lỗi:
- Mức đang làm là IDEA nhưng `Boot.prefab` đặt `stage: 1`. Theo bảng giai đoạn IDEA, mở game vào thẳng trận, "Chơi lại" vào thẳng trận mới và không có "Về màn bắt đầu". Bản đang chạy theo PROTOTYPE.
- Kỉ lục cũ trong PlayerPrefs (`Run/Best` = 9) không được chuyển sang `record.save`; kỉ lục bắt đầu lại từ 0. Gói thiết kế Run đã đọc không đòi chuyển. Máy nào đã chơi bản cũ sẽ mất kỉ lục cũ.
- Nền trận có các cột chữ nhật hồng và khối trắng ở đường chân trời (ảnh `02-battle1-start.png`), trông như hình giữ chỗ. Tester chưa đối chiếu với thiết kế Battlefield.

## Cấu trúc thư mục
- README.md: file này
- images/01-home-record0.png: màn bắt đầu khi chưa có bản lưu, "Kỉ lục: 0 đợt"
- images/02-battle1-start.png: đầu trận 1
- images/03-rating-invite.png: lời mời đánh giá nằm trên popup thua trận 1
- images/04-lose1-popup.png: popup thua trận 1: 3 đợt, kỉ lục 3, "Kỉ lục mới!" (lỗi 1)
- images/05-lose2-popup.png: popup thua trận 2: 1 đợt, kỉ lục 3, không có dải kỉ lục mới
- images/06-home-after-restart.png: màn bắt đầu sau khi Stop rồi Play lại, "Kỉ lục: 3 đợt"
