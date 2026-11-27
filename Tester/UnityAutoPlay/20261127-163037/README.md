# CheatPanel IDEA (chạy lại)
Tester: UnityAutoPlay · Đợt: 20261127-163037

## Mục đích
@CodeLead giao (tin `m20261127-162837-2ab5f8`), mức IDEA, thay đợt `20261127-162312` bị ngắt khi server khởi động lại. Chơi thử nút và panel cheat: nút ở mọi scene, nội dung panel, chọn dòng state, chọn lại dòng đang chạy, "Chơi bình thường", nằm trên popup, khoá dòng lúc Loading, lỗi console.

## Test gì
- Feature `CheatPanel`: `live/feature-cheat-panel/cheat-access/access-spec`, `live/feature-cheat-panel/cheat-states/states-rules`.
- 8 dòng state của panel: Chọn đội, Chuyển scene, Dịch vụ popup, HUD trận, Màn bắt đầu, Popup mời đánh giá, Popup thua, Thanh nâng cấp.
- Scene `Loading`, `Game`.

## Môi trường
- Commit `8419355`; `git status UnityProject` sạch sau khi chơi.
- Unity Editor (bản dev), Game view 1920x1080, bắt đầu Play từ `Assets/_Composition/Loading.unity`. Mọi asset installer để cheat `None`.
- Cách bấm: gửi sự kiện click qua EventSystem (raycast tại toạ độ để kiểm chặn chạm) hoặc `onClick` của đúng nút. Không có chạm tay thật.
- Editor chạy rất chậm khi không được focus (có lúc khoảng 2 giây một khung). Đoạn chuyển scene được đo bằng cách chạy từng khung (`EditorApplication.Step`). Hai ảnh chụp lúc khung chậm không thấy nút Cheat trên màn tải; chạy từng khung cho thấy đó chỉ là khung đổi scene.

## Các bước
1. Play. Mong: có nút chữ "Cheat". Thấy: nút màu xám đậm trên màn bắt đầu (ảnh 01). **Đạt.**
2. Bấm nút. Mong: nút đổi nhìn, panel tiêu đề "Cheat", "Chơi bình thường" đứng đầu và được đánh dấu, dòng "tên feature: tiêu đề state" xếp A tới Z. Thấy: nút màu cam; 1 + 8 dòng đúng dạng và đúng thứ tự: Chọn đội, Chuyển scene, Dịch vụ popup, HUD trận, Màn bắt đầu, Popup mời đánh giá, Popup thua, Thanh nâng cấp (ảnh 02). **Đạt.** Dòng "Dịch vụ popup" dài quá hai hàng nên bị cắt bằng "…".
3. Panel mở, raycast tại nút "Chơi", góc màn hình và vùng tiêu đề rồi bấm; bấm lại nút Cheat. Mong: nền che chặn chạm, chạm nền không đóng, nút đóng panel. Thấy: raycast chỉ trúng `cheat-shade`, `cheat-panel` hoặc dòng của panel; panel vẫn mở; bấm nút thì đóng, nút về màu cũ. **Đạt.** (Raycast ngay trong khung vừa mở panel còn trúng nút "Chơi" vì panel chưa được vẽ; từ khung sau thì bị chặn.)
4. Chọn "HUD trận". Mong: panel đóng, scene tải lại qua SceneFlow, state được dựng ngay khi scene chạy, mở lại thấy dòng được đánh dấu. Thấy: panel đóng, có màn tải, scene `Game` đổi handle, dòng HUD được đánh dấu (ảnh 05), nhưng màn hình dừng ở màn bắt đầu bình thường, kỉ lục 3 (ảnh 06). **Không đạt phần dựng state.** Bấm "Chơi" rồi "Vào trận" thì HUD mới hiện đợt 7, thành 300/1000, 245 tiền, Băng giá còn 12 (ảnh 07).
5. Chạm lại dòng HUD đang chạy. Mong: dựng lại từ đầu. Thấy: scene tải lại lần nữa (handle -46404 sang -47890). **Đạt** phần tải lại.
6. Trong lúc đang tải ở bước 5: bấm nút Cheat rồi raycast và bấm một dòng state và dòng "Chơi bình thường". Mong: nút vẫn mở panel, các dòng khoá. Thấy: panel mở, 9/9 dòng khoá và mờ (ảnh 04); chạm dòng không làm gì; tải xong thì 9 dòng mở lại. **Đạt.**
7. "Chọn đội". Thấy: màn chọn đội mở ngay, đủ 3 chỗ, "Vào trận" sáng (ảnh 10); mở panel thấy dòng này được đánh dấu. **Đạt.**
8. "Popup thua". Thấy: popup thua mở ngay, 16 đợt, "Kỉ lục mới" (ảnh 11); dòng được đánh dấu. **Đạt.**
9. Popup thua đang mở: raycast tại nút Cheat rồi bấm; raycast tại "Chơi lại" và "Về màn bắt đầu". Mong: nút và panel nằm trên popup, popup không nhận chạm. Thấy: nút Cheat ở trên cùng; panel phủ popup (ảnh 03); raycast tại hai nút của popup trúng dòng của panel. **Đạt.**
10. "Popup mời đánh giá". Thấy: popup mời mở ngay, chưa chạm nút nào (ảnh 12). **Đạt.**
11. "Thanh nâng cấp". Mong: state giữa trận dựng ngay. Thấy: dừng ở màn bắt đầu bình thường (ảnh 08). **Không đạt.** Bấm "Chơi" rồi "Vào trận" thì thanh nâng cấp mới hiện 5 mục mua được, 1 mục thiếu tiền (ảnh 09).
12. Đang trong trận, chọn "Dịch vụ popup". Thấy: scene tải lại, popup mẫu mở ngay (ảnh 13). **Đạt.**
13. "Màn bắt đầu". Thấy: màn bắt đầu với kỉ lục ép 15 đợt (ảnh 14). **Đạt.**
14. "Chuyển scene". Thấy: chuyển sang scene `Loading`, thanh dừng 40%, "Lỗi tải: Wallet" (ảnh 15); nút Cheat còn đó, mở panel được, 9 dòng không khoá, dòng này được đánh dấu (ảnh 16). **Đạt.**
15. Từ màn Loading đó chọn "Chơi bình thường". Mong: game chạy lại từ scene đầu, không còn state. Thấy: chạy `Loading` rồi `Game`, màn bắt đầu kỉ lục thật 3 đợt (ảnh 17), mở panel thấy "Chơi bình thường" được đánh dấu. Kỉ lục ép 16 và 15 không đè kỉ lục thật. **Đạt.**
16. Chọn "Chơi bình thường" lần nữa và chạy từng khung. Mong: lúc Loading có nút, dòng khoá. Thấy: khung 1 tới 50 ở scene `Loading` có nút, dòng khoá; khung 51 đổi scene không có nút; khung 52 tới 101 ở scene `Game` dưới màn tải có nút, dòng khoá; khung 102 hết màn tải, dòng mở lại (ảnh 18). **Đạt.**
17. Cuộn: 8 state hiện tại chưa dài quá panel (nội dung 680, khung 728). Lúc Play nhân thêm 8 dòng tạm (chỉ trong bộ nhớ) cho nội dung cao 1224, rồi kéo dọc bắt đầu trên một dòng. Thấy: danh sách cuộn (y 0 tới 458), chữ không bị thu nhỏ (ảnh 19). **Đạt** với dòng tạm.
18. Đọc console. Thấy: không có error hay warning. **Đạt.**
19. Stop Play.

Chưa kiểm: `cheat_enabled` tắt và thiếu config (phải sửa asset), bản build trên máy, bản không có state nào, state cheat `PanelOpen` của chính CheatPanel (phải chọn trên asset), các khung đầu tiên lúc vừa bấm Play (đã đo cùng luồng Loading qua "Chơi bình thường").

## Kết quả
không đạt: nút, panel, danh sách dòng, đánh dấu, chặn chạm, khoá lúc tải, "Chơi bình thường" và 6/8 state đạt; chọn "HUD trận" và "Thanh nâng cấp" chỉ ra màn bắt đầu, state chỉ hiện sau khi người test tự bấm "Chơi" rồi "Vào trận"; console sạch.

## Lỗi tìm thấy
- **Hai state trận không dựng ngay khi chọn trong panel.** Thiết kế (`states-rules`): tải lại scene chứa state "và feature sở hữu dựng state ngay khi scene chạy". Tái hiện: bước 4 (ảnh 06, 07) và bước 11 (ảnh 08, 09). Feature sở hữu: `Hud` (state "Đợt 7…") và `HudUpgrade` (state "Giữa trận…"). Với `Hud`, code (`HudSession.ApplyCheatOnceBattleRuns`) chỉ áp state khi `RunStep.Playing`, mà scene `Game` mở ở màn bắt đầu. Với `HudUpgrade` chỉ mới thấy hành vi, chưa đọc code. Có thể cần cả luồng `Run`/`Home` để vào thẳng trận. Theo code, chọn state trên asset rồi Play đi cùng đường này (chưa chơi thử).
- Nhẹ: chữ dòng "Dịch vụ popup: …" dài quá hai hàng nên bị cắt bằng "…", không đọc hết tiêu đề state (ảnh 02). Thuộc `CheatPanel` (cỡ dòng) hoặc tiêu đề state của dịch vụ `Popup` quá dài.

## Cấu trúc thư mục
- README.md: file này
- images/: ảnh chụp
- 01-nut-dong-man-bat-dau.png: nút Cheat khi panel đóng, trên màn bắt đầu
- 02-panel-mo.png: panel mở, 9 dòng, "Chơi bình thường" được đánh dấu
- 03-panel-tren-popup-thua.png: panel mở đè trên popup thua
- 04-dong-khoa-khi-dang-tai.png: panel mở lúc đang tải, mọi dòng khoá
- 05-dong-hud-duoc-danh-dau.png: dòng "HUD trận" được đánh dấu khi mở lại
- 06-chon-hud-van-o-man-bat-dau.png: sau khi chọn "HUD trận" vẫn là màn bắt đầu
- 07-hud-chi-dung-sau-vao-tran.png: state HUD hiện sau khi bấm "Chơi", "Vào trận"
- 08-chon-nang-cap-van-o-man-bat-dau.png: sau khi chọn "Thanh nâng cấp" vẫn là màn bắt đầu
- 09-nang-cap-chi-dung-sau-vao-tran.png: state thanh nâng cấp hiện sau khi vào trận
- 10-state-chon-doi.png: state "Chọn đội"
- 11-state-popup-thua.png: state "Popup thua"
- 12-state-popup-danh-gia.png: state "Popup mời đánh giá"
- 13-state-popup-mau.png: state "Dịch vụ popup"
- 14-state-man-bat-dau-ki-luc.png: state "Màn bắt đầu"
- 15-state-loading-loi.png: state "Chuyển scene" (Loading lỗi)
- 16-panel-tren-loading.png: panel mở trên scene Loading
- 17-choi-binh-thuong.png: sau "Chơi bình thường", kỉ lục thật 3
- 18-nut-tren-man-tai.png: nút Cheat trên màn tải lúc chạy lại
- 19-cuon-voi-dong-tam.png: danh sách đã cuộn, có 8 dòng tạm của tester
