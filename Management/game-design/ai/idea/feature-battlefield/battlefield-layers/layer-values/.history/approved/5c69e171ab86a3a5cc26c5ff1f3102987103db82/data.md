---
based-on: 9c6afd246afa
refs: feature-castle/castle-core/castle-body-values@3e635d304ab3, feature-hud/hud-layout/layout-spec@aa79f655e44b
---
# Thông số các lớp nền

Nền vẽ theo camera đang có của scene Game: 1u bằng 60 px, mặt đất ở px 840. Trời, cảnh xa, dải đất phủ x từ −21 tới +21u; decor là 10 hoạ tiết phẳng hai bên thành.

## Ý cốt lõi

### view-camera | AI 8 | Nền vẽ theo camera đang có: 1u = 60 px, mặt đất y=0 ở px 840, màn 16:9 thấy x −16…+16u, y −4…+14u
Camera là của scene Game, không đổi vì nền; nền bám theo.

### cover-width | AI 8 | Trời, cảnh xa và dải đất phủ x từ −21 tới +21u
Màn rộng giữ chiều cao 18u nên thấy rộng ra hai bên; 21:9 thấy khoảng ±21u. Phủ tới đó thì không hở mép.

### sky-values | AI 8 | Trời là một hình phủ từ y=0 tới y=14u, đứng yên
Phần trời trên y=11.7u nằm sau dải HUD trên, vẫn được vẽ.

### far-values | AI 7 | Cảnh xa là một dải liền đứng trên mặt đất, cao 2.5u (dự kiến), đứng yên
Thấp hơn thành (4u) để thành nổi lên giữa màn; cao hơn decor để đọc được là cảnh phía sau.

### ground-values | AI 8 | Dải đất phủ từ y=−4u lên tới mặt đất y=0
Phần đất dưới y=−1.3u nằm sau thanh nâng cấp và khay kĩ năng, vẫn được vẽ; mặt đất và 1.3u đất ngay dưới nó luôn thấy được.

### decor-ten | AI 7 | Decor gồm 10 hoạ tiết phẳng: mỗi bên 3 cỏ và 2 đá, tâm ở x ±4, ±6.5, ±9, ±11.5, ±14u
Cỏ ở ±4, ±9, ±14; đá ở ±6.5, ±11.5. Cỏ rộng 0.8u, đá rộng 0.6u, cùng cao 0.3u, chân trên y=0. Hai bên đối xứng.

### decor-clear-castle | AI 8 | Không món decor nào có phần nằm trong khoảng x −3…+3u
Khoảng này là thân thành và chỗ hero xuất phát, hồi sinh; để trống cho unit đọc rõ.

### hud-clear | AI 8 | Dải HUD trên chỉ che trời, thanh dưới chỉ che đất: mặt đất, cảnh xa, decor và thành không bị HUD che
Kiểm bằng hai đường y=11.7u (px 140) và y=−1.3u (px 920) trên frame.
