# Chụp art Castle
Tester: UnityAutoPlay · Đợt: 20261127-183811

## Mục đích
@CodeLead giao (tin `m20261127-183428-1ebca8`, việc `w20261127-183433-unityautoplay`): chụp art Castle cho người xem. Xem thành, đạn shot và sét thunder có hiện bằng ảnh PNG Simple không, hay ra ô vuông màu GreyBox; ghi lỗi console. Không kiểm feature khác.

## Test gì
- Feature `Castle`: ba art `Castle/Body`, `Castle/Shot`, `Castle/Thunder`. Không đối chiếu số liệu thiết kế.
- Scene `Assets/_Composition/Game.unity`, chơi thường: "Chơi", "Vào trận" với đội mặc định (Hiệp sĩ, Đấu sĩ rìu, Cung thủ). Không dùng state cheat.

## Môi trường
- Commit `3a1e2a4`, cộng thay đổi art Castle chưa commit trong working tree (`FeatureAssets/Public/Art/Castle`, code Castle, `_Composition/Game.Art.asset`). `git status UnityProject` sau khi chơi giống lúc bắt đầu.
- Unity Editor (bản dev). Ảnh 01 và 03 cỡ 1280x720, ảnh 04 tới 06 cỡ 1920x1080.
- Bấm nút bằng `onClick` của đúng nút, không có chạm tay thật.
- Editor chỉ chạy khoảng 1 khung mỗi giây. Để bắt được đạn, sét và lúc thành bị đánh, tester đổi lúc Play `Time.timeScale` (0 tới 3) và `Time.captureDeltaTime` (0.04 tới 0.1), bật `Application.runInBackground`, rồi cho Editor tạm dừng đúng khung cần chụp. `timeScale` và `captureDeltaTime` đã trả về 1 và 0 trước khi Stop. Vì vậy đợt này không đánh giá độ mượt, thời lượng hiệu ứng hay cảm giác lúc chơi thật.
- Không xoá được console lúc đầu: guard chặn `read_console clear`.

## Các bước
1. Mở `Game.unity`, Play, bấm "Chơi", bấm "Vào trận". Mong: vào trận, thành là ảnh Simple. Thấy: thành là lâu đài hai tháp, cờ đỏ, cổng gỗ (ảnh 01). Sprite lúc chạy là `Features/Castle/FeatureAssets/Public/Art/Castle/Body/Simple.png`. **Đạt: ảnh PNG, không phải GreyBox.**
2. Chờ thành bắn nhiều phát. Mong: đạn là ảnh Simple. Thấy: đạn là viên tròn màu cam vàng có viền tối, không phải ô vuông, bay bên trái thành (ảnh 04, quanh điểm 685,715). Sprite là `.../Art/Castle/Shot/Simple.png`. Đạn nhỏ, khoảng 18 px trên ảnh 1920x1080; ảnh 06 có thêm một viên cạnh tháp phải. **Đạt: ảnh PNG, không phải GreyBox.**
3. Lúc Sấm sét sẵn sàng và có quái trên sân, bấm nút "Sấm sét". Mong: dùng được, sét là ảnh Simple. Thấy: dùng được; tia sét trắng viền xanh hiện trên một con sói bên trái, nút chuyển sang hồi chiêu 20 (ảnh 05). Sprite là `.../Art/Castle/Thunder/Simple.png`. Ảnh chụp ngay đầu hiệu ứng (sét còn đang phóng to), chữ tiền "+4" đè một phần lên tia sét. **Đạt: ảnh PNG, không phải GreyBox.**
4. Chờ quái tới đánh thành. Mong: thành vẫn là ảnh Simple khi bị đánh. Thấy: máu thành 821/1000, cả ảnh lâu đài nháy đỏ và đang lắc ngang (vị trí Body lệch x=0.05), vẫn giữ đủ chi tiết của PNG (ảnh 06). Ảnh 03 là một lần nháy đỏ trước đó, máu 990/1000. **Đạt.**
5. Đọc console (error, warning) lúc đang chơi và sau khi Stop. Thấy: không có error. Có đúng 1 warning của công cụ unity-mcp, không phải của game: `MCP-FOR-UNITY: Suppressed duplicate command resent on a new connection; awaiting the in-flight result instead of running it twice.` **Đạt.**
6. Stop Play.

Chưa kiểm: thành tối màu khi dưới nửa máu (máu thấp nhất đã thấy là 821/1000), thành sụp, nhịp nảy khi nâng cấp, Băng giá và Cổ vũ (ngoài yêu cầu).

Khi xem ảnh: các cột hồng và khối trắng trên dải đất không thuộc Castle (object `flag-*`, `fence-*` của `Battlefield`, không được kiểm trong đợt này).

## Kết quả
đạt: thành, đạn shot và sét thunder đều hiện ảnh PNG Simple, không cái nào ra GreyBox; thành bị đánh vẫn là ảnh Simple (nháy đỏ, lắc); console không có lỗi.

## Lỗi tìm thấy
Trong game: không.

Ngoài game, thấy khi chạy lệnh:
- `python Management/tools/art.py list Castle` in "Chưa có art Simple hay Detail nào của Castle: game đang dùng GreyBox", trái với game (cả ba đang dùng Simple). Theo code `art.py`: id art phải viết thường (`ID_RX`), còn ba thư mục và khoá của Castle là `Body`, `Shot`, `Thunder`, nên lệnh bỏ qua chúng. Suy ra từ code, chưa thử: tab Studio và `art.py choose` cũng có thể không thấy ba art này. Thuộc `Castle` (đặt tên id) hoặc `art.py`.

## Cấu trúc thư mục
- README.md: file này
- images/: ảnh chụp
- 01-toan-canh.png: toàn cảnh đầu trận, thành là ảnh Simple
- 03-dan-shot.png: thành nháy đỏ khi bị đánh, máu 990/1000; dù tên file là dan-shot, ảnh này KHÔNG thấy đạn của thành (đạn xem ảnh 04)
- 04-dan-shot.png: đạn shot (viên tròn cam vàng) đang bay bên trái thành
- 05-sam-set.png: dùng Sấm sét, tia sét trên con sói bên trái, nút đang hồi 20
- 06-thanh-bi-danh.png: thành bị đánh, nháy đỏ, máu 821/1000, có một viên đạn cạnh tháp phải
