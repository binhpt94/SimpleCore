---
based-on: e1440a2a0de0
refs: feature-castle/castle-upgrade-effects@2c1e443d0ced, feature-hero/hero-respawn@2ae1008b33cd, feature-hero/hero-list/hero-roster-values@8a2346250094
---
# Thông số thân thành

Vị trí, kích thước, máu của thành và điểm xuất phát của hero. Thành là một khối 2D đứng yên ở giữa dải đất, vào trận có 1000 máu, không tự hồi, máu về 0 là thua ngay.

## Ý cốt lõi

### castle-box | AI 8 | Thành là khối 2D đứng yên, tâm ở x=0u, rộng 3u, cao 4u; mép thành ở x=-1.5u và x=+1.5u
Mọi khoảng cách tới thành (tầm quái, tầm tự bắn, vùng Sấm sét) tính từ mép thành, không từ tâm. Đã loại: tính từ tâm (tầm thay đổi khi đổi kích thước hình).

### castle-hp-value | AI 8 | Máu tối đa lúc vào trận là 1000, máu hiện tại bắt đầu bằng 1000
Một con Lính đợt 1 (5 sát thương/1.0s) phải đứng đánh 200s mới phá được thành; áp lực đến từ số đông và các đợt sau. Không tự hồi; chỉ nâng cấp máu (`feature-castle/castle-upgrade-effects`) làm máu tăng.

### castle-destroyed-once | AI 9 | Máu hiện tại nhỏ hơn hoặc bằng 0 thì thành bị phá, báo `feature-run` đúng 1 lần
Sau khi bị phá, thành không nhận thêm sát thương, không tự bắn, không cho dùng kĩ năng. Nhiều đòn cùng lúc đưa máu xuống dưới 0 vẫn chỉ báo 1 lần; máu hiện ra là 0, không âm.

### castle-hero-points | AI 7 | Hero xuất phát và hồi sinh ở mép thành: chỗ 1 và 3 ở x=-1.5u, chỗ 2 ở x=+1.5u
Chỗ theo thứ tự hero trong đội đã chọn ở `feature-team-select`. Hai hero ở bên trái, một bên phải; hero sẽ tự đi theo quái gần nhất nên chỗ đứng ban đầu chỉ để đội không chồng lên nhau. Đã loại: cả đội ở x=0 (chồng hình, khó đếm hero).

### castle-hit-feedback | AI 7 | Thành bị đánh thì nháy 0.1s; bị phá thì thu nhỏ còn 0 trong 0.5s
Người chơi thấy ngay thành đang bị đánh dù không nhìn thanh máu. Nháy nhiều đòn liên tiếp thì đặt lại 0.1s, không cộng dồn.
