# Thông số thân thành

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Đặt một thành 2D đứng yên ở giữa dải đất, chân trên mặt đất (y=0).
- Giữ máu tối đa và máu hiện tại; nhận sát thương từ quái; báo `feature-run` khi bị phá.
- Cho `feature-hero/hero-respawn` biết điểm xuất phát của từng chỗ trong đội.
- Nhận lệnh "tăng máu" từ `feature-castle/castle-upgrade-effects` (cộng vào cả máu tối đa và máu hiện tại).

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Tâm thành | x=0 | u | Thành ở giữa màn hình (màn thấy x=-16u tới +16u) |
| Kích thước | rộng 3 × cao 4 | u | Bộ số chung; đủ lớn để nhận ra giữa các unit cao 0.8-3.5u |
| Mép thành | x=-1.5 và x=+1.5 | u | Mọi tầm tính từ đây |
| Máu tối đa lúc vào trận | 1000 | máu | Dự kiến; xem cân bằng ở "Chưa rõ" |
| Tự hồi | 0 | máu/s | Luật vòng 3 |
| Điểm xuất phát chỗ 1, 2, 3 | -1.5 / +1.5 / -1.5 | x (u) | Chia hai bên, không chồng toàn bộ |
| Nháy khi bị đánh | 0.1 | s | Đủ thấy, không nhấp nháy liên tục khó chịu |
| Thu nhỏ khi bị phá | 0.5 | s | Cho người chơi thấy khoảnh khắc thua trước khi `feature-run` báo |
| Máu khi hồi sinh | 30% máu tối đa lúc đó, làm tròn lên | máu | Người chọn (2026-11-24); 1000 máu tối đa → 300 |
| Số lần hồi sinh mỗi lượt | 1 | lần | Giữ nghĩa của thua và kỉ lục |
| Hiện lại khi hồi sinh | phóng từ 0 lên đủ cỡ trong 0.5 | s | Ngược với lúc bị phá, cùng thời gian |

Cân bằng dự kiến: đợt 10 có 26 quái, Lính 40×1.9=76 máu, 5×1.9=9.5 sát thương/1.0s. 10 Lính đứng đánh thành cùng lúc trừ 95 máu/s, thành 1000 máu trụ khoảng 10.5s: đủ để người chơi kịp bấm Sấm sét và mua máu.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Máu tối đa lúc vào trận | 1000 | 1000 (chỉnh sau khi chơi thử) | theo số đã chỉnh |
| Hình thành | 1 sprite gen bằng AI | 1 sprite, có hình khi máu dưới 50% | đủ hình theo mức máu (100%, 50%, 25%) |
| Phản hồi bị đánh | nháy 0.1s | nháy 0.1s + rung nhẹ 0.1s | đủ hiệu ứng và âm thanh |
| Phản hồi bị phá | thu nhỏ còn 0 trong 0.5s | như IDEA | hiệu ứng sụp đổ riêng |
| Hồi sinh 30% máu | không có (chưa có quảng cáo); bị phá là thua | có, mỗi lượt 1 lần | có, mỗi lượt 1 lần |

## Trường hợp biên
- Nhiều quái đánh trúng cùng một khung hình: trừ lần lượt; lần đưa máu về ≤0 là lần báo bị phá; các lần sau bị bỏ qua.
- Nâng máu đúng lúc máu về 0 cùng khung hình: bị phá xử lý trước; đã bị phá thì không mua được nữa (`feature-run` dừng trận).
- Máu hiện tại có số lẻ (sát thương quái nhân hệ số đợt): giữ số thực; còn 0.4 máu thì thành chưa bị phá. Cách hiện số là việc của `feature-hud` (làm tròn xuống, nên hiện "0/1000" và thanh còn một vệt).
- Tạm dừng: thành không nhận sát thương vì mọi quái đứng yên; hiệu ứng nháy đang chạy cũng dừng.
- Hero và đòn của chính thành, kĩ năng thành không bao giờ trừ máu thành.
- Hồi sinh mà quái vẫn đứng sát thành: không có lúc bất tử; quái đánh lại ngay khi trận chạy tiếp. 300 máu chịu được khoảng 3s trước 10 Lính đợt 10, đủ để bấm Sấm sét. Đã loại: bất tử vài giây (thêm luật, che mất áp lực).
- Hồi sinh rồi bị phá lần hai: báo `feature-run` thêm 1 lần; lần này là thua.
- **Hồi sinh: thành phóng từ 0 lên đủ cỡ trong 0.5 giây; máu đặt 30% máu tối đa ngay đầu 0.5 giây đó.** Ngược với lúc bị phá (thu nhỏ còn 0 trong 0.5 giây). `feature-run` chờ thành hiện xong rồi mới cho trận chạy tiếp. (Chuyển từ ý revive-anim của nhánh thứ tự hồi sinh ở popup hồi sinh, 2026-11-26: việc này do thành làm.)

## Đã tự quyết
- **Mọi tầm tính từ mép thành.** Khớp bộ số chung ("tầm 6u tính từ mép thành"). Đã loại: tính từ tâm.
- **Điểm xuất phát 2 trái 1 phải theo thứ tự chỗ.** Một quy tắc cố định, dễ kiểm. Đã loại: xuất phát về phía có quái gần nhất (lúc vào trận chưa có quái nên vô nghĩa).
- **Thành không quyết cách hiện số máu (2026-11-25, sửa lệch).** Bản trước ghi "hiện làm tròn lên", trái với nhánh thông tin trận của `feature-hud` (làm tròn xuống: thấy "0" là biết rất nguy). HUD là nơi hiện nên HUD quyết; thành chỉ giữ số thực và báo bị phá.
- **Bị phá chỉ báo 1 lần.** Tránh `feature-run` nhận hai lần thua.

- **Frame `castle.frame` (2026-11-23).** Node này giữ các hằng chung của chiến trường: `castle-left`, `castle-right`, `castle-w`, `castle-h`, `screen-left`, `screen-right`; node khác `use` lại. Số tính ra trong frame: hộp hero ở chỗ 1/3 có x=-2u = tâm -1.5u − nửa rộng 0.5u; chỗ 2 có x=1u = +1.5u − 0.5u. Hộp hero chồng lên thành (cờ `overlap`) là đúng ý: điểm xuất phát nằm ở mép thành.

## Chưa rõ
- 1000 máu có thể dư ở đợt đầu (Lính đợt 1 cần 200s một mình): giữ theo bộ số chung, đề xuất chỉnh sau chơi thử xuống 600-800 nếu đợt 1-5 không thấy áp lực.
