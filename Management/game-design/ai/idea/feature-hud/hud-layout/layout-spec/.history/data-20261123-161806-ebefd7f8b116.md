---
based-on: e541be0cca19
refs: feature-hud-upgrade/bar-layout@4a9d06c74198, feature-popup/popup-layer@91d4f017d2fb, feature-hud/hud-status/status-spec@9899b000bd73
---
# Chỗ của từng phần HUD

Chốt mép và góc cho từng phần HUD trận: thông tin và nút tạm dừng ở mép trên, 3 nút kĩ năng ở góc dưới bên phải, phần còn lại của mép dưới cho thanh nâng cấp.

## Ý cốt lõi

### top-row-order | AI 7 | Mép trên từ trái sang phải: nút tạm dừng, tiền, máu thành, đợt
Máu thành ở giữa, thẳng phía trên thành. Nút tạm dừng ở góc trên bên trái, xa góc dưới bên phải nơi có nút kĩ năng.

### skills-bottom-right | AI 7 | 3 nút kĩ năng gom thành một cụm ở góc dưới bên phải, xếp ngang theo thứ tự Sấm sét, Băng giá, Cổ vũ
Ngón cái phải chạm tới. Cụm này không đổi chỗ trong trận.

### upgrade-gets-rest | AI 7 | Thanh nâng cấp (`feature-hud-upgrade`) dùng phần mép dưới từ cạnh trái tới sát cụm kĩ năng
Hai bên không chồng lên nhau ở bất kì tỉ lệ màn hình nào.

### strip-never-covered | AI 8 | Không phần HUD nào chồng lên dải chiến trường, ở mọi tỉ lệ màn hình ngang từ 16:9 tới 21:9
Kiểm bằng cách so khung của từng phần HUD với khung dải chiến trường: không giao nhau.

### safe-area | AI 7 | Mọi phần HUD nằm trong vùng an toàn của máy (không bị tai thỏ, góc bo hay thanh điều hướng che)
