# Thay ô màu tạm bằng art Codex đã giao

Người giao (2026-11-24): art theo order đã có đủ, thay vào game. Người làm: phiên Castle, Enemy, Hero, HudUpgrade, mỗi phiên tự làm phần của mình.

## Order đã giao (`studio.py status`), mọi item đang "Chưa duyệt"
| Feature | Order | Item |
|---|---|---|
| Castle | `Castle/castle-sprites` v1 | castle, shot, thunder |
| Enemy | `Enemy/enemy-sprites` v1 | enemy-soldier, enemy-wolf, enemy-giant, enemy-archer, enemy-boss |
| Hero | `Hero/hero-sprites` v1 | hero-knight, hero-axeman, hero-swordsman, hero-archer, hero-mage |
| HudUpgrade | `HudUpgrade/bar-icons` v1 | hero-knight, hero-axeman, hero-swordsman, hero-archer, hero-mage, castle-health, castle-attack, castle-skill, coin |

Mọi item đã hết lượt gen. Mức 0 sẽ không chạy lại được nếu không có phản hồi; mỗi item làm lại tối đa 2 lần.

## Việc của mỗi phiên
1. Xem ảnh ở `Resources/<Feature>/codex/<id>/v1/`, đo bằng `python Management/tools/studio.py measure <Feature>/<id>`.
2. Chấm từng item bằng `python Management/tools/studio.py review <Feature>/<id> <item> <0-3> "<phản hồi>"`.
   - Mức 1 đến 3: ảnh được chép vào `FeatureAssets/<deliver>/Generated/<id>/`. Refresh Unity để có `.meta`.
   - Mức 0: ghi phản hồi rõ, rồi nhắn Codex làm lại item đó.
3. Thay ô màu tạm trong view hoặc prefab bằng sprite vừa chép. Kích thước trên chiến trường giữ theo thiết kế (`gd_pack.py <Tên>`). Giá trị nào là cấu hình thì gán qua config ở `FeatureAssets/Private`.
4. Biên dịch, đọc console, chạy `validate.py --feature <Tên>`, rồi báo cáo.
5. @CodeLead Play lại toàn trận để kiểm hình.

## Chưa làm ở đây
- Ảnh concept `_design/...` là do người chấm trên tab Game design, không thuộc việc này.
