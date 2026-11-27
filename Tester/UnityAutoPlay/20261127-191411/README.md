# Chup art Hero va Enemy
Tester: UnityAutoPlay · Đợt: 20261127-191411

## Mục đích
@CodeLead giao (tin `m20261127-191351-de9b9f`, việc `w20261127-191354-unityautoplay`): chụp art Hero và Enemy cho người xem. Mỗi tướng, mỗi quái phải hiện ảnh PNG Simple, không phải ô vuông màu GreyBox; ghi lỗi console. Castle đã kiểm ở đợt khác, không kiểm lại.

## Test gì
- Feature: Hero (hình trong trận), Enemy (hình trong trận).
- Thiết kế: `live/feature-hero/hero-list` (5 tướng), `live/feature-enemy/enemy-list/enemy-type-values` (5 loại quái).
- Scene `Assets/_Composition/Game.unity`.
- Cheat lúc Play (qua `execute_code`, chỉ đổi trạng thái lúc chạy): `WaveDirector.StartWave(7)` rồi `StartWave(10)` (của `IWaveCheat`); `CastleCheat.SetHealth(1000)` một lần ở đợt 10 để thành sống tới khi boss ra; đổi `Time.timeScale` và phóng camera để chụp. Đổi đội bằng chính các thẻ ở màn Chọn đội.

## Môi trường
- Commit `217f1e5` cộng thay đổi chưa commit trong cây làm việc (gồm art Hero và Enemy ở `FeatureAssets/Public/Art`).
- Unity Editor (bản dev), Play từ `Game.unity`, ảnh 1280x720.
- `art.py list`: 5 mục Hero và 5 mục Enemy đều "đang dùng Simple".
- `_Composition/Boot.prefab` đang để `stage: 1` (PROTOTYPE).

## Các bước
1. Mở `Game.unity`, đọc console. Mong: không lỗi. Thấy: 0 lỗi, 1 cảnh báo biên dịch (mục Lỗi tìm thấy).
2. Play, bấm "Chơi". Mong: màn Chọn đội với đội mặc định Hiệp sĩ, Đấu sĩ rìu, Cung thủ. Thấy đúng (ảnh 01).
3. Bấm "Vào trận". Mong: 3 tướng của đội mặc định hiện ảnh. Thấy: Hiệp sĩ, Đấu sĩ rìu, Cung thủ đều là ảnh người, sprite lấy từ `Features/Hero/FeatureAssets/Public/Art/Hero/{knight,axeman,archer}/Simple.png` (ảnh 02, 03, 04).
4. Đợt 1. Mong: Lính hiện ảnh. Thấy: ảnh lính giáp tối cầm đao, `Art/Enemy/soldier/Simple.png` (ảnh 04).
5. Nhảy tới đợt 7. Mong: có thêm Khổng lồ, Cung thủ quái, Sói bằng ảnh. Thấy đủ 4 loại cùng lúc, mỗi loại một ảnh riêng, đường dẫn `Art/Enemy/{soldier,giant,archer,wolf}/Simple.png` (ảnh 05, 06).
6. Nhảy tới đợt 10, chờ con cuối đợt. Mong: boss hiện ảnh. Thấy: Trùm (#44) hiện ảnh quỷ giáp đỏ cầm rìu, `Art/Enemy/boss/Simple.png`, hộp 3 x 3.5u (ảnh 07, 08).
7. Stop, Play lại, bỏ Hiệp sĩ và Đấu sĩ rìu khỏi đội, thêm Kiếm khách và Pháp sư, "Vào trận". Mong: 2 tướng còn lại hiện ảnh. Thấy: Kiếm khách và Pháp sư là ảnh, `Art/Hero/{swordsman,mage}/Simple.png` (ảnh 09, 10).
8. Đọc console (lỗi và cảnh báo) lúc ở đợt 7, giữa đợt 10, trước khi Stop lần 1 và lần 2. Thấy: 0 mục ở mọi lần.
9. Stop Play.

## Kết quả
đạt: cả 5 tướng và cả 5 loại quái đều hiện ảnh PNG Simple trong trận, không tướng hay quái nào ra GreyBox; không có lỗi console.

| Tướng / quái | Hiện gì | Ảnh |
|---|---|---|
| Hiệp sĩ (`Hero/knight`) | Simple | 03, 08 |
| Đấu sĩ rìu (`Hero/axeman`) | Simple | 03, 08 |
| Cung thủ (`Hero/archer`) | Simple | 03, 10 |
| Kiếm khách (`Hero/swordsman`) | Simple | 10 |
| Pháp sư (`Hero/mage`) | Simple | 10 |
| Lính (`Enemy/soldier`) | Simple | 04, 06 |
| Sói (`Enemy/wolf`) | Simple | 06 |
| Khổng lồ (`Enemy/giant`) | Simple | 06, 08 |
| Cung thủ quái (`Enemy/archer`) | Simple | 06, 08 |
| Trùm (`Enemy/boss`) | Simple | 07, 08 |

Cho người xem ảnh:
- Hình ngả đỏ là hiệu ứng nháy khi bị đánh, không phải GreyBox.
- Ô hồng đứng và ô trắng ngang ở nền là cờ và hàng rào GreyBox của Battlefield (`Battlefield/flag-*`, `fence-*`), không phải tướng hay quái.
- Vệt đòn, đạn và số "+5" là hiệu ứng vẽ bằng hình đơn giản hoặc chữ; đợt này không kiểm art của chúng.

## Lỗi tìm thấy
Không có lỗi về art Hero, Enemy; không có lỗi console.
- Cảnh báo biên dịch (không phải lỗi), thấy ở bước 1, feature Enemy: `Assets/Features/Enemy/Scripts/View/EnemyStageView.cs(307,29): warning CS0108: 'EnemyStageView.DestroyObject(Object)' hides inherited member 'Object.DestroyObject(Object)'. Use the new keyword if hiding was intended.`

## Cấu trúc thư mục
- README.md: file này
- images/01-chon-doi.png: màn Chọn đội với đội mặc định
- images/02-vao-tran-dot1.png: vào trận đợt 1, toàn cảnh với đội mặc định
- images/03-ba-tuong-can-canh.png: cận cảnh Cung thủ, Hiệp sĩ, Đấu sĩ rìu
- images/04-linh-va-tuong.png: cận cảnh 3 tướng mặc định và một Lính ở mép phải
- images/05-dot7-toan-canh.png: toàn cảnh đợt 7
- images/06-linh-khonglo-cungthu-soi.png: cận cảnh Lính, Khổng lồ, Cung thủ quái, Sói
- images/07-boss-toan-canh.png: toàn cảnh đợt 10, Trùm bên trái
- images/08-boss-can-canh.png: cận cảnh Trùm, Khổng lồ, Cung thủ quái, Lính, Hiệp sĩ, Đấu sĩ rìu
- images/09-doi2-vao-tran.png: vào trận với đội Kiếm khách, Pháp sư, Cung thủ
- images/10-kiemkhach-cungthu-phapsu.png: cận cảnh Kiếm khách, Cung thủ, Pháp sư
