# PopupRating IDEA
Tester: UnityAutoPlay · Đợt: 20261127-031927

## Mục đích
@CodeLead nhờ (tin m20261127-031913-8c7770, người quyết 2026-11-27): chơi thử PopupRating ở mức IDEA sau khi feature kiểm 42/42 và nối vào Boot.Game. Xem 6 điểm: thời điểm lời mời so với popup thua, không mời khi không phá kỉ lục, huỷ khi chạm popup thua sớm, Để sau, không mời lại trong một lần Play, chạm Đánh giá trong Editor.

## Test gì
- Feature PopupRating (popup `PopupRating/Main`), cùng Run, PopupLose, Castle, dịch vụ Popup.
- Node: `live/feature-popup-rating/rating-timing/timing-spec`, `live/feature-popup-rating/rating-invite/invite-spec`, `feature-run/run-flow/flow-rules` (popup thua sau 1 giây), `feature-run/run-score/score-rules`.
- State: thành bị phá bằng `CastleFallCheat.Fall()`; kỉ lục mới bằng cách đặt `RunScore.Score = Best + 1` trong bộ nhớ ngay trước khi phá thành (không có cheat sẵn cho việc phá kỉ lục). Để đo lại test 1, 2, 3 trong cùng lần Play, đặt `RatingParticipant.InvitesShown = 0` trong bộ nhớ (ghi rõ ở từng bước).
- Bấm nút bằng `Button.onClick.Invoke()` (không qua EventSystem). Đo thời gian bằng `Time.realtimeSinceStartup` trong `Canvas.willRenderCanvases`, ghi lúc `RunFlow.BattleStopped` và lúc con mới của `PopupLayer` xuất hiện.
- Scene: Play từ `Assets/_Composition/Loading.unity` → Game → Chọn đội → trận.

## Môi trường
- Commit 9fe31c45 (main, có thay đổi chưa commit trong cây làm việc), Unity Editor, bản dev (UNITY_EDITOR).
- Kỉ lục lưu (PlayerPrefs `Run/Best`) lúc đầu là 13; game tự lưu lên 19 sau các lượt phá kỉ lục của đợt này.

## Các bước
1. Lượt đầu (kỉ lục 13 → 14), phá thành bằng lệnh execute_code. Mong: popup thua 1.0s, lời mời 1.5s, lời mời nằm trên. Thấy: lời mời mở trước (PopupLayer xuất hiện 0.64s), popup thua sau, **lời mời nằm dưới popup thua** (ảnh 01). Chạm Để sau trên lời mời: không có tác dụng (không ở trên cùng). Chạm Chơi lại trên popup thua: về Chọn đội, **lời mời vẫn còn, nổi trên màn Chọn đội** (ảnh 02); lúc này Để sau mới đóng được.
2. Test 5 (đã mời một lần, chơi lại, kỉ lục 14 → 15): popup thua hiện, không có lời mời, `InvitesShown` giữ 1 (ảnh 03). **Đạt.**
3. Đặt `InvitesShown = 0`, lượt mới, phá thành bằng lệnh execute_code (kỉ lục → 16): lời mời 0.614s, popup thua 0.866s, lời mời lại nằm dưới (ảnh 04: chỉ thấy popup thua). Lặp lại lỗi bước 1.
4. Test 1, đo sạch: đặt `InvitesShown = 0`, hẹn phá thành từ trong vòng khung hình (khung thứ 120, khung dài nhất 0.010s, ~280 fps), kỉ lục → 17. Thấy: popup thua +0.977s, lời mời +1.502s, lời mời nằm trên popup thua (ảnh 05). **Đạt.** Ảnh lúc popup thua chưa có lời mời: ảnh 06 (sau Để sau) và 04 cho thấy popup thua với dòng "Kỉ lục mới!".
5. Test 4: chạm Để sau. Lời mời đóng, popup thua còn nguyên (ảnh 06). Chạm Chơi lại: về Chọn đội, tức popup thua bấm được. **Đạt.**
6. Test 2: đặt `InvitesShown = 0`, lượt điểm 0. Popup thua +0.981s, `IsNewBest = false`, không có lời mời sau 4 giây (ảnh 07). **Đạt.**
7. Test 3: lượt phá kỉ lục (→ 18), hẹn chạm Chơi lại 1.2s sau khi thành bị phá (popup thua đã hiện ở +0.988s). Thấy: `BattleCleared` lúc +1.202s, hết đếm, không có lời mời, `InvitesShown` giữ 0 (ảnh 08). Lượt sau phá kỉ lục (→ 19): popup thua +0.998s, lời mời +1.502s nằm trên, `InvitesShown = 1` (ảnh 09). **Đạt.**
8. Test 6: chạm Đánh giá. Lời mời đóng, popup thua bên dưới còn nguyên (ảnh 10), Editor gọi `Application.OpenURL("https://play.google.com/store/apps/details?id=com.DefaultCompany.UnityProject")` (mã gói mặc định của project). Console không có lỗi hay cảnh báo. **Đạt.**
9. Console (lỗi và cảnh báo) cả đợt, đọc sau khi Stop Play: 0 dòng.

## Kết quả
không đạt: 6 điểm CodeLead nêu đều đạt khi khung hình đều, nhưng khi có một khung dài (Editor khựng) lời mời mở trước và nằm dưới popup thua, kẹt không bấm được, rồi còn sót lại trên màn Chọn đội.

## Lỗi tìm thấy
- **Lời mời nằm dưới popup thua và bị sót lại khi có khung hình dài** (bước 1 và 3; ảnh 01, 02, 04; feature PopupRating, có thể liên quan Run).
  - Tái hiện: trong trận, phá kỉ lục rồi phá thành bằng một lệnh làm Editor đứng vài trăm ms (execute_code). Lời mời mở ở +0.61s, popup thua ở +0.87s; trong `PopupLayer` thứ tự là `PopupRatingMain` (sib 1) rồi `LoseMain` (sib 3).
  - Hậu quả: Để sau và Đánh giá không có tác dụng (không ở trên cùng); chạm Chơi lại thì `BattleCleared` chỉ dừng đếm, không đóng lời mời đã mở, nên lời mời nổi trên màn Chọn đội.
  - Trái luật: "Lời mời luôn nằm trên popup thua, không bao giờ nằm dưới" (`rating-timing`).
  - Nghi nguyên nhân (chưa kiểm): `UnityRatingClock` cộng `Time.unscaledDeltaTime` không giới hạn, còn độ trễ 1 giây của popup thua chạy theo đồng hồ khác của Run (`SimClockRunDelay`?) có giới hạn bước, nên một khung dài làm đồng hồ của lời mời chạy vượt. Trên máy thật, khung dài lúc mở popup thua (tải, GC, máy chậm) có thể gây ra như vậy.
- Ghi chú nhỏ, không phải lỗi: popup thua hiện ở +0.977 tới +0.998s, lời mời +1.502s, khớp 1.0s và 1.5s.

## Cấu trúc thư mục
- README.md: file này
- images/01-invite-over-lose.png: bước 1, lần đầu: chỉ thấy popup thua, lời mời đã mở nhưng nằm dưới
- images/02-after-replay.png: bước 1, sau Chơi lại: lời mời sót lại trên màn Chọn đội
- images/03-test5-no-reinvite.png: bước 2, test 5: phá kỉ lục lần nữa, không mời lại
- images/04-test1-final.png: bước 3: popup thua có "Kỉ lục mới!", lời mời lại nằm dưới
- images/05-test1-invite-on-lose.png: bước 4, test 1 đo sạch: lời mời nằm trên popup thua ở +1.5s
- images/06-test4-after-later.png: bước 5, test 4: sau Để sau, popup thua còn nguyên
- images/07-test2-score0-no-invite.png: bước 6, test 2: lượt điểm 0, không có lời mời
- images/08-test3-cancelled.png: bước 7, test 3: chạm Chơi lại ở +1.2s, không có lời mời
- images/09-test3-next-invited.png: bước 7, test 3: lượt sau phá kỉ lục vẫn được mời
- images/10-test6-after-rate.png: bước 8, test 6: sau Đánh giá, lời mời đóng, popup thua còn nguyên
