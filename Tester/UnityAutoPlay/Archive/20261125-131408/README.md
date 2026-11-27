# IDEA quai bi ha bien mat sau Choi lai
Tester: UnityAutoPlay · Đợt: 20261125-131408

## Mục đích
@Manager giao ngày 2026-11-25, mức IDEA: kiểm bản sửa một lỗi người báo.
- Lỗi: sau khi bấm Chơi lại, quái bị hạ không biến mất, vì phần hình của Enemy bị kẹt ở trạng thái dừng.
- Enemy đã sửa, test EditMode 764/764 qua.
- Đợt 20261125-115759 không bắt được lỗi này vì chỉ đếm object ở đầu lượt.

## Test gì
- Feature: Enemy (phần hình `EnemyStageView`: nhún khi đi, thu nhỏ rồi huỷ khi bị hạ), Hero (nhún khi đi), Run (Chơi lại).
- Node `live`: `feature-enemy/enemy-stats/enemy-stat-values`, `feature-hero/hero-list/hero-roster-values` (hiệu ứng nhún), `feature-run/run-start/replay-checks`.
- Scene: `Assets/_Composition/Loading.unity` rồi Play, game tự sang `Game.unity`.
- Để thua nhanh, `execute_code` gọi `CastleBody.ForceHealth(1)` (cùng đường với `ICastleCheat.SetHealth`), rồi để quái tự đánh vỡ thành.

## Môi trường
- Commit HEAD lúc mở phiên: 2bd8f00, cộng các thay đổi chưa commit của hôm nay, gồm bản sửa Enemy (guard chặn lệnh git trong phiên tester).
- Unity 6000.3.10f1, Play trong Editor (bản dev), Game view `1920x1080 Landscape`.
- Cách đo:
  - một hàm gắn vào `Application.onBeforeRender` (chạy mỗi khung hình, chỉ trong bộ nhớ, không ghi file);
  - mỗi khung so số con của `EnemyStage(Clone)` với `EnemyField.AliveCount`;
  - theo dõi tỉ lệ scale từng hình quái, thời điểm bắt đầu thu nhỏ và lúc bị gỡ;
  - chụp ảnh lần đầu thấy một quái thu còn dưới 0.75 lần.
- Giới hạn môi trường: phần lớn thời gian Editor chạy Play ở khoảng 1 khung hình/giây (`unscaledDeltaTime` ≈ 1.0, `deltaTime` bị kẹp 0.333), có lẽ vì cửa sổ Unity không được focus.
  - Ở mức này cả lần thu nhỏ 0.3 giây nằm gọn trong một khung, nên không thấy được hình thu nhỏ.
  - Khung hình lên lại khoảng 60/giây trong một đoạn của lượt 1 và lượt 3 (sau khi người bấm vào cửa sổ Unity); số đo thu nhỏ lấy từ các đoạn đó.

## Các bước
Viết tắt: MĐ là mong đợi theo thiết kế, TT là thực tế thấy.

1. **Lượt 1: Play từ Loading, để hero hạ quái.**
   - MĐ: mỗi quái thu nhỏ rồi biến mất trong khoảng 0.3 giây; hero và quái nhún khi đi.
   - TT:
     - 8 quái của đợt 1 bị hạ, cả 8 hình đều rời `EnemyStage`.
     - Chạy ở 1 khung/giây nên không thấy khung thu nhỏ nào.
     - Nhún khi đi: y của quái dao động 0.00–0.10u, của hero 0.00–0.10u (thiết kế 0.1u).
   - Kết quả: đạt phần huỷ và nhún; chưa đo được thu nhỏ.
2. **Ép thành còn 1 máu, quái phá thành, popup thua, bấm Chơi lại.**
   - TT: lượt 1 kết thúc ở bước Lost, popup có nút Chơi lại; bấm Chơi lại thì vào lượt 2.
   - Kết quả: đạt.
3. **Lượt 2: hạ vài quái, đếm GameObject quái trước và sau.**
   - TT về số quái bị hạ: 13 hình quái bị gỡ khỏi `EnemyStage` trong lượt.
   - TT về hình còn sót: không khung nào có hình dư quá 1 khung (số khung liên tiếp có con nhiều hơn số quái sống tối đa là 1, chỉ 2 khung có dư).
   - TT khi đếm toàn scene: object tên `Enemy …` gồm cả object ẩn là 6, không có object ẩn; khớp 6 con của `EnemyStage` và 6 quái sống trong logic. Hình của quái đã hạ bị huỷ thật, không chỉ ẩn.
   - TT về nhún: y quái 0.05–0.09u, y hero 0.017–0.087u lúc đang đi.
   - Ở 1 khung/giây nên không thấy hình thu nhỏ.
   - Sau đó ép thành 1 máu, thua, bấm Chơi lại.
   - Kết quả: đạt phần huỷ và nhún.
4. **Lượt 3: như lượt 2.**
   - TT về số quái bị hạ: 8 hình quái bị gỡ, 5 lần trong đó bắt được cả đoạn thu nhỏ.
   - TT về thời gian từ lúc bắt đầu thu nhỏ tới lúc bị gỡ:
     - 0.31 giây, 0.29 giây, 0.27 giây, 0.27 giây, với scale nhỏ nhất 0.00–0.06 lần;
     - 1 lần 0.86 giây (scale nhỏ nhất 0.27) rơi vào lúc khung hình tụt về 1/giây, nên thời gian thật bị kéo dài, không phải hình kẹt.
   - TT về hình còn sót: hình dư tối đa 18 khung liên tiếp, khoảng 0.3 giây ở 60 khung/giây, đúng là khoảng đang thu nhỏ; sau đó luôn về 0.
   - TT khi đếm cuối lượt: object `Enemy …` là 2, ẩn 0; `EnemyStage` 2 con; logic còn 2 quái sống.
   - TT về nhún: y quái 0.00–0.10u, hero 0.099–0.100u ở đỉnh nhún.
   - Kết quả: đạt.
5. **Ảnh quái đang thu nhỏ.**
   - TT: bắt được ở lượt 3, không phải lượt 2 (lượt 2 chạy 1 khung/giây). Khung 1742, quái thu còn 0.72 lần; trong ảnh là Lính gần Đấu sĩ rìu, x khoảng 1655, nhỏ hơn các Lính khác.
   - Console: 0 lỗi, 0 cảnh báo suốt đợt và sau khi Stop Play.
   - Kết quả: đạt.

## Kết quả
đạt: sau mỗi lần Chơi lại, quái bị hạ đều thu nhỏ rồi bị huỷ thật, không còn hình kẹt hay object ẩn ở lượt 2 và 3; nhún khi đi đúng 0.1u; console không lỗi. Hình thu nhỏ chỉ đo được ở các đoạn Editor chạy đủ khung hình.

## Lỗi tìm thấy
không. Lỗi người báo (quái bị hạ không biến mất sau Chơi lại) không tái hiện ở lượt 2 và lượt 3.

## Cấu trúc thư mục
- README.md: file này
- images/02-run2-enemy-shrinking.png: ảnh lúc một Lính đang thu nhỏ (còn 0.72 lần, cạnh Đấu sĩ rìu, x khoảng 1655). Thực tế chụp ở lượt 3; tên file đặt sẵn cho lượt 2 nhưng lượt 2 chạy 1 khung/giây nên không bắt được.
