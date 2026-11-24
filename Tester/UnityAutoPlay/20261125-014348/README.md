# chot IDEA
Tester: UnityAutoPlay · Đợt: 20261125-014348

## Mục đích
@Manager giao: chơi thử scene Game ở mức IDEA để xác nhận các node IDEA còn chờ kiểm, cho @CodeLead đánh `done`. Đội mặc định Hiệp sĩ, Đấu sĩ rìu, Cung thủ; tiền đầu 50; chỉ có kĩ năng Sấm sét; không có hồi sinh.

## Test gì
- Feature: Castle, Hero, Hud, HudUpgrade, Upgrade, Run, PopupLose, _Services/SessionScene.
- Node `live/...`:
  - feature-castle/castle-skills/skill-values
  - feature-hud/hud-skill-button/skill-button-spec
  - feature-hero/hero-stats/hero-level-values
  - feature-upgrade/upgrade-effect/effect-numbers
  - feature-upgrade/upgrade-purchase/purchase-rules
  - feature-castle/castle-upgrade-effects/upgrade-effect-values
  - feature-run/run-flow/flow-rules
  - feature-run/run-score/score-rules
  - feature-popup-lose/lose-result/result-spec
  - feature-popup-lose/lose-actions/actions-spec
  - feature-session-scene/session-clean/clean-checks
- State cheat: Hud `mid-battle`, HudUpgrade `mixed-buttons`, PopupLose `new-record`, Popup `sample-popup`, SceneFlow `load-error`.
- Scene: mở Play từ `Assets/_Composition/Loading.unity`, tự sang scene Game. Mức IDEA nên vào thẳng trận.

## Môi trường
- Unity Editor, Play mode, bản dev (`UNITY_EDITOR`).
- Commit git: không lấy được. Guard chặn lệnh `git log` của phiên tester. Branch `main` lúc mở phiên.
- Cách thao tác:
  - Bấm nút bằng `Button.onClick.Invoke()` qua `execute_code`, như một chạm của người chơi.
  - Đọc số từ các lớp logic đang chạy bằng reflection, chỉ đọc.
  - Ghi diễn biến từng khung hình bằng `EditorApplication.update`, rồi ghi CSV vào `images/`.
- Ép trạng thái lúc Play, không lưu asset nào:
  - `CastleCheat.SetHealth(1)` ở lượt 2, 3, 4 để thành bị phá nhanh. Manager cho phép.
  - Chạy `UpgradeBarCheat.Apply` trên shop và ví đang chạy để dựng state `mixed-buttons`.
- Không sửa scene, asset, code hay cài đặt.

## Các bước
Lượt 1, kĩ năng và nâng cấp:
1. Sấm sét khi có quái gần thành.
   - Mong: quái trong 6u mất 80 máu; nút hồi chiêu đếm từ 20 giây.
   - Thấy:
     - Người khổng lồ ở x=-5,36 (cách mép thành 2,86u) mất đúng 80 máu (120 → 40).
     - Hồi chiêu bắt đầu 20 giây; nút hiện "20" kèm lớp phủ.
     - Hiệu ứng sét có mặt đúng chỗ con quái, tồn tại 0,3 giây.
     - Đếm ngược 20 → 1, làm tròn lên. Lớp phủ vơi đều (cao 140 → 70 ở giữa → 0).
     - Đúng 20,0 giây thì sẵn sàng: ẩn số giây, ẩn lớp phủ, nút nảy 1,0 → 1,2 → 1,0 trong 0,3 giây.
     - Ảnh 01, CSV 02. **Đạt.**
2. Bấm Sấm sét lần nữa khi đang hồi (cùng khung hình).
   - Mong: không có gì xảy ra.
   - Thấy: hồi chiêu giữ 20, máu quái không đổi. **Đạt.**
   - Thêm: sau khi thành bị phá, bấm Sấm sét cũng không có tác dụng (hồi chiêu 0, máu 14 con quái không đổi).
3. Nâng một hero.
   - Mong: trừ 30 xu; hero phóng 1,2 lần rồi về trong 0,3 giây; nút lên Cấp 2, giá 41.
   - Thấy:
     - Lượt 1: Hiệp sĩ đang gục; 764 → 734 xu, nút "Cấp 2 / 41". Đúng luật "hero gục chỉ đổi cấp".
     - Lượt 2: mua Hiệp sĩ đang đứng. Thân phóng 0,3906 → 0,4687 (đúng 1,2 lần) ở 0,15 giây, về 0,3906 ở 0,30 giây.
     - Máu Hiệp sĩ tăng đúng phần tăng: +45 mỗi cấp, 15% của 300.
     - CSV 03 (lượt 1, sampler bắt đầu trễ 0,33 giây nên không có dữ liệu phóng to), 04, 08. **Đạt.**
4. Nâng máu thành và sức đánh thành (giá 40).
   - Mong: trừ 40 xu; thành phóng 1,1 lần trong 0,3 giây; máu tối đa +150, sát thương tự bắn +2.
   - Thấy:
     - Máu thành: 734 → 694 xu; 149,5/1000 → 299,5/1150; nút "Cấp 2 / 56".
     - Sức đánh: trừ 40 xu, AttackLevel 2, sát thương tự bắn 10 → 12, nút "Cấp 2 / 56".
     - Thân thành 0,8065 → 0,8867 (1,0995 lần) ở 0,15 giây, về 0,8065 ở 0,30 giây.
     - Lượt 2 nâng máu 2 lần: máu tối đa 1300.
     - CSV 09. **Đạt.**
5. Bấm khi thiếu tiền, và bấm nhanh hai lần.
   - Mong: thiếu tiền thì không mua; bấm nhanh chỉ mua một lần.
   - Thấy:
     - 22 xu bấm sức đánh (giá 56): không mua, xu và cấp giữ nguyên. Nút thiếu tiền mờ (alpha 0,4).
     - 36 xu, bấm Cung thủ (giá 30) hai lần trong cùng khung hình: chỉ mua một cấp. Còn 6 xu, Cấp 2, giá mới 41.
     - Khi đủ tiền cho cả hai lần (140 xu), hai chạm mua hai cấp liên tiếp (30 rồi 41), đúng luật "mỗi chạm một yêu cầu".
     - CSV 08, 09. **Đạt.**

Lượt 2, 3, 4 (thua và chơi lại):

6. Để thành bị phá.
   - Mong: 1 giây sau hiện popup thua với số đợt đã dọn, kỉ lục, chỉ có nút "Chơi lại".
   - Thấy:
     - Lượt 1: thành tự bị phá ở đợt 8. Popup: "Thành đã bị phá", "Trụ được 7 đợt", "Kỉ lục: 7 đợt", "Kỉ lục mới!" trên dải nền, chỉ có nút "Chơi lại".
     - Trận đứng yên sau khi thua: vị trí 14 con quái không đổi sau khoảng 20 giây; thanh nâng cấp ẩn.
     - Lượt 2 (ép 1 máu): từ lúc bị phá tới popup đúng 1,000 giây; điểm 6 = số đợt đã dọn.
     - Lượt 4 đo bằng cả hai đồng hồ: 1,000 giây giờ game và 1,002 giây giờ thực.
     - Ảnh 06, 10, 13; CSV 10, 15. **Đạt.**
7. Bấm "Chơi lại" hai lần thật nhanh.
   - Mong: vào trận mới một lần với đội mặc định, tiền 50, đợt 1.
   - Thấy:
     - Lượt 1 → 2: lần 1 đóng popup và dựng trận mới ngay trong khung hình đó. Lần 2 ở khung hình sau không còn nút để bấm.
     - Lượt 2 → 3: bấm hai lần trong cùng khung hình, chỉ một trận mới được dựng. Từ khung hình sau chỉ còn 1 bộ Heroes, 0 popup.
     - Mỗi lần: 50 xu, "Đợt 1", 1000/1000, Hiệp sĩ, Đấu sĩ rìu, Cung thủ.
     - CSV 07, 11. **Đạt.**
8. Thua và chơi lại thêm (tổng 4 lượt, 4 lần vào trận mới).
   - Mong: mỗi lượt bắt đầu sạch, console không lỗi mới.
   - Thấy:
     - Đầu lượt 3 về đúng giá trị đầu lượt: cấp 1 cả 6 mục, giá 30/30/30/40/40/60, sát thương tự bắn 10, máu tối đa 1000, Sấm sét sẵn sàng, cấp kĩ năng 1, 3 hero đủ máu.
     - Số transform ngay đầu lượt 3 và lượt 4 bằng nhau (67), 12 root, 0 popup, 1 quái, 3 hero. Lúc trước khi chơi lại là 83 transform, 42 quái.
     - Console chỉ có một cảnh báo do cheat Hud tự in (bước Cheat). Không có lỗi.
     - Ảnh 12; CSV 11; txt 14. **Đạt.**
9. Lượt có điểm vượt kỉ lục.
   - Mong: popup hiện "Kỉ lục mới!".
   - Thấy:
     - Lượt 1: điểm 7 vượt kỉ lục 0, có "Kỉ lục mới!".
     - Lượt 2 (6 < 7) và lượt 3 (3 < 7): không có dòng đó, kỉ lục giữ 7.
     - Ảnh 06, 10, 13. **Đạt.**

Cheat (mỗi lần một cái):

10. Hud `mid-battle`, bấm nút "Cheat: mid-battle" trên HUD ở lượt 5.
    - Thấy: tiền 245, thành 300/1000 (thanh còn 30%), "Đợt 7" kèm thông báo đợt "Đợt 7", Sấm sét sẵn sàng.
    - Console báo bỏ qua Băng giá và Cổ vũ, đúng ghi chú "Ép ở IDEA" của node cheat.
    - Ảnh 16, txt 16. **Đạt.**
11. HudUpgrade `mixed-buttons`.
    - Không bật được qua config (đó là sửa asset). Thay vào đó, chạy chính code cheat `UpgradeBarCheat.Apply(120, [3,5,1,2,4,3])` trên shop và ví đang chạy. `FindTeamMismatch` báo đội khớp (knight, axeman, archer).
    - Thấy: 120 xu; Hiệp sĩ Cấp 3/55, Đấu sĩ rìu Cấp 5/100, Cung thủ Cấp 1/30, máu thành Cấp 2/56, sức đánh Cấp 4/110 sáng; kĩ năng Cấp 3/135 mờ. Khớp thiết kế.
    - Phần đi dây qua installer chưa kiểm. Ảnh 17, txt 17. **Đạt (phần hiển thị).**
12. PopupLose `new-record`: **chưa kiểm, cần phiên sở hữu bật**.
    - Cheat chỉ chạy khi chọn trong installer asset lúc cài đặt.
    - Các thành phần của popup phá kỉ lục đã thấy ở popup thật lượt 1 (ảnh 06), nhưng với số 7/7, không phải 16/16.
13. Popup `sample-popup`: **chưa kiểm, cần phiên sở hữu bật**. Cheat chỉ chạy khi chọn trong config lúc cài đặt.
14. SceneFlow `load-error`: **chưa kiểm, cần phiên sở hữu bật**. Cheat chỉ chạy khi chọn trong installer asset của scene Loading.

Ghi chú:
- Lỗi engine "PlayerLoop internal function has been called recursively" không xuất hiện (lọc console theo "PlayerLoop": 0 dòng).
- Nền chiến trường (Battlefield) chưa code, không kiểm.
- Lượt 3 có một lần đo ra 0,502 giây từ lúc thành bị phá tới popup. Nguyên nhân là đo sai:
  - Đo bằng `Time.time` của game, trong khi bộ hẹn giờ chờ dùng giờ thực (`ignoreTimeScale`).
  - Đúng lúc đó tester gọi `execute_code`, khiến Editor khựng nên giờ game chạy chậm hơn giờ thực.
  - Đo lại ở lượt 4 bằng cả hai đồng hồ (giờ game và giờ thực) và không chen lời gọi nào: 1,000 / 1,002 giây. Không phải lỗi của game.

## Kết quả
đạt: cả 9 bước của Manager và 11 node vòng 4 đều đúng thiết kế ở mức IDEA. Cheat Hud `mid-battle` và HudUpgrade `mixed-buttons` (phần hiển thị) đạt. Ba cheat PopupLose `new-record`, Popup `sample-popup`, SceneFlow `load-error` chưa kiểm vì chỉ bật được qua config asset.

## Lỗi tìm thấy
không

Còn mở (không phải lỗi): 3 cheat chỉ bật qua config asset (PopupLose, `_Services/Popup`, SceneFlow), cần phiên sở hữu hoặc @CodeLead bật để kiểm.

## Cấu trúc thư mục
- README.md: file này
- images/01-thunder-cooldown.png: ảnh ngay sau khi bấm Sấm sét, nút hiện "20" kèm lớp phủ (lượt 1)
- images/02-thunder-cooldown.csv: hồi chiêu Sấm sét từng khung hình trong 21,5 giây: thời gian còn lại, số hiện, lớp phủ, cỡ nút (nảy 1,2 lần lúc hồi xong)
- images/03-upgrade-pop.csv: cỡ thân Hiệp sĩ và thành sau khi mua ở lượt 1; sampler bắt đầu trễ nên không bắt được hiệu ứng (Hiệp sĩ đang gục)
- images/04-upgrade-pop-standing.csv: lần đo thứ hai ở lượt 1, cũng bắt đầu trễ 0,33 giây, không có dữ liệu phóng to (giữ để đối chiếu)
- images/06-lose-popup-run1.png: popup thua lượt 1: "Trụ được 7 đợt", "Kỉ lục: 7 đợt", "Kỉ lục mới!", nút "Chơi lại"
- images/07-replay-double-tap.csv: bấm "Chơi lại" ở hai khung hình liền nhau (lượt 1 → 2): popup, hero, quái, tiền, đợt, máu thành theo từng khung hình
- images/08-run2-hero-buy-doubletap.csv: lượt 2, mua Hiệp sĩ hai chạm (đủ tiền cho cả hai) và cỡ thân phóng 1,2 lần trong 0,3 giây; chạm nâng máu thành
- images/09-run2-castle-pop-insufficient-doubletap.csv: lượt 2, nâng sức đánh (10 → 12), thành phóng 1,1 lần trong 0,3 giây, chạm khi thiếu tiền bị từ chối
- images/10-run2-lose-delay.csv: lượt 2, thời điểm thành bị phá và popup hiện (trễ 1,000 giây), điểm 6, kỉ lục 7
- images/10-lose-popup-run2.png: popup thua lượt 2: "Trụ được 6 đợt", "Kỉ lục: 7 đợt", không có "Kỉ lục mới!"
- images/11-replay-run3-same-frame.csv: bấm "Chơi lại" hai lần trong cùng khung hình (lượt 2 → 3): chỉ một trận mới, root, tổng transform
- images/12-run3-start.png: đầu lượt 3: 50 xu, Đợt 1, 1000/1000, mọi nút Cấp 1
- images/13-lose-popup-run3.png: popup thua lượt 3: "Trụ được 3 đợt", "Kỉ lục: 7 đợt"
- images/14-run3-lose-and-run4-start.txt: lượt 3 thua, chữ trên popup, số đối tượng trước khi chơi lại và ngay đầu lượt 4 (67 transform, 0 popup)
- images/15-run4-lose-delay-realtime.csv: lượt 4, từng khung hình từ lúc thành bị phá tới popup, giờ game và giờ thực (1,000 / 1,002 giây)
- images/16-cheat-hud-mid-battle.png: HUD sau cheat `mid-battle`: 245 xu, 300/1000, Đợt 7, thông báo đợt
- images/16-cheat-hud-mid-battle.txt: chữ HUD trước và sau cheat `mid-battle`
- images/17-cheat-hudupgrade-mixed-buttons.png: thanh nâng cấp sau `UpgradeBarCheat.Apply`: năm nút sáng, nút kĩ năng mờ
- images/17-cheat-hudupgrade-mixed-buttons.txt: cách gọi cheat HudUpgrade lúc Play và kết quả từng nút
