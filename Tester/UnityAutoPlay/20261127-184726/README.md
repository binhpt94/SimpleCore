# Chụp art Castle bản Simple
Tester: UnityAutoPlay · Đợt: 20261127-184726

## Mục đích
@CodeLead giao (tin `m20261127-183428-1ebca8`, việc `w20261127-183433-unityautoplay`): chụp art Castle cho người xem. Xem thành, đạn shot và sét thunder hiện ảnh PNG Simple hay ô vuông GreyBox; ghi lỗi console. Không kiểm feature khác.
Cùng việc này đã có đợt `20261127-183811` chạy trước. Đợt đó chụp code Castle cũ (id `Body`, `Shot`, `Thunder`).

## Test gì
- Feature `Castle`: art `Body`, `Shot`, `Thunder`; sau lần nạp lúc 18:48:44 là `body`, `shot`, `thunder`.
- State cheat: "HUD trận: Đợt 7 vừa bắt đầu, thành còn 30% máu, Băng giá đang hồi" (bước 7, 8). Các bước khác chơi bình thường.
- Scene `Assets/_Composition/Game.unity`.

## Môi trường
- Commit `3a1e2a4`, kèm thay đổi chưa commit của Castle và `_Composition/Game.Art.asset`. Play trong Editor (bản dev).
- Worker `w20261127-184720-castle` nạp 12 file và xoá 9 file trong `Assets` lúc **18:48:44, giữa lượt Play 1**. Lượt Play 1 bắt đầu bằng code cũ. Lượt Play 2 chạy code mới.
- Tester chỉnh `Time.timeScale`, `isPaused` và gắn hàm tạm vào `EditorApplication.update` để dừng đúng khung hình. `timeScale` đã trả về 1 trước khi Stop. Editor chạy rất chậm (khoảng 1 khung mỗi lệnh).

## Các bước
1. Mở `Game.unity`, Play, bấm "Chơi", "Vào trận". Mong: thành là ảnh Simple. Thấy: sprite `Art/Castle/Body/Simple.png`, 512x512, đúng ảnh lâu đài (`01`).
2. Chờ thành tự bắn. Mong: đạn là ảnh Simple. Thấy: object `Shot` dùng `Art/Castle/Shot/Simple.png`, trên màn là viên tròn màu cam (`02`).
3. Bấm "Sấm sét". Mong: sét là ảnh Simple. Thấy: object `Thunder` dùng `Art/Castle/Thunder/Simple.png`, trên màn là tia sét; nút chuyển sang hồi chiêu 20 (`03`).
4. Chơi tiếp. Thấy: trận đứng hình. `Time.time` tăng từ 54s tới 68s, quái, sét và số hồi chiêu không đổi, sự kiện `CastleView.Ticked` không còn ai nghe (`07`). Nguyên nhân: Unity nạp lại code giữa lúc Play sau lần nạp của worker (`Editor.log`: "Reloading assemblies after finishing script compilation").
5. Stop, Play lại bằng code mới, "Chơi", "Vào trận". Mong: ba art vẫn là Simple. Thấy: `_castleSprite`, `_shotSprite`, `_thunderSprite` lấy từ `Art/Castle/{body,shot,thunder}/Simple.png` (`05`).
6. `art.py list Castle`. Thấy: `Castle/body`, `Castle/shot`, `Castle/thunder` đều "đang dùng Simple".
7. Chọn cheat "HUD trận". Thấy: đợt 7, thành 300/1000, ảnh thành nhuộm tối (`06`).
8. Chờ quái đánh thành. Mong: thành bị đánh vẫn là ảnh Simple. Thấy: máu 295/1000, ảnh thành nháy đỏ, một viên đạn shot đang bay bên phải (`04`).
9. Đọc console, Stop Play.

Sét thunder chỉ chụp ở lượt Play 1 (code cũ). Ở code mới chỉ kiểm sprite đã nạp đúng, chưa bấm Sấm sét lại.

## Kết quả
đạt: thành, đạn shot và sét đều là ảnh PNG Simple, không cái nào ra GreyBox; lần nạp code giữa lúc Play làm đứng trận lượt 1 và để lại lỗi console về thư mục trùng tên khác hoa thường.

| Art | Hiện gì | Ghi chú |
|---|---|---|
| Thành | ảnh Simple | cả code cũ và code mới |
| Đạn shot | ảnh Simple | thấy trên màn ở cả hai lượt Play |
| Sét thunder | ảnh Simple | chụp ở code cũ; code mới nạp đúng sprite |

## Lỗi tìm thấy
- Console, 1 error: "The following files have the same name as another file, but only differ in casing and will be ignored", gồm `Art/Castle/Body` và `body`, `Shot` và `shot`, `Thunder` và `thunder`. Thấy sau lần nạp 18:48:44; lúc mở scene console không có error hay warning. Trên đĩa `Body/`, `Shot/`, `Thunder/` còn lại và rỗng; file nằm ở `body/`, `shot/`, `thunder/`. Thuộc: art của `Castle` và bước nạp của `worker.py` (suy ra: nạp xoá file nhưng không xoá thư mục rỗng). Lúc chơi lại ba sprite vẫn nạp đúng.
- Console, 3 warning đi kèm: "A meta data file (.meta) exists but its folder 'Assets/Features/Castle/FeatureAssets/Public/Art/Castle/body' can't be found, and has been created", và tương tự cho `shot`, `thunder`.
- Console, 1 warning của unity-mcp: "Suppressed duplicate command resent on a new connection". Không thuộc game.
- Công cụ: `worker.py` nạp code vào `Assets` lúc 18:48:44 trong khi lượt chơi thử này đang Play, làm đứng trận (bước 4, ảnh `07`). Theo `unity.md`, lúc tester đang chơi thử thì không bên nào được ghi vào `Assets`. Thuộc: công cụ (`worker.py`, khoá Editor).
- Ảnh và ba art Castle: không thấy lỗi.

## Cấu trúc thư mục
- README.md: file này
- images/: ảnh chụp
- images/01-toan-canh.png: toàn cảnh có thành, đầu trận (code cũ)
- images/02-dan-shot.png: đạn shot (viên cam) bay bên trái thành (code cũ)
- images/03-set-thunder.png: sét thunder đánh quái bên trái, nút Sấm sét đang hồi 20 (code cũ)
- images/04-thanh-bi-danh.png: thành bị đánh, nháy đỏ, máu 295/1000, kèm một viên đạn shot (code mới)
- images/05-toan-canh-code-moi.png: toàn cảnh sau khi chơi lại bằng code mới
- images/06-thanh-duoi-nua-mau.png: thành còn 300/1000, ảnh nhuộm tối (code mới, cheat HUD trận)
- images/07-tran-dung-sau-nap-lai.png: trận đứng hình sau khi Unity nạp lại code giữa lúc Play
