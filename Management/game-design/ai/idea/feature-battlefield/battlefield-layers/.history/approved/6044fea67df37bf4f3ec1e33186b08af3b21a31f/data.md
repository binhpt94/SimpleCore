---
based-on: ac9f64988525
refs: feature-castle/castle-core@81d0b06388c9, glossary#battlefield@565b01770cea
---
# Các lớp nền và thứ tự vẽ

Nền gồm bốn lớp: trời, cảnh xa, dải đất, decor. Node này chốt mỗi lớp là gì, nằm đâu so với mặt đất và mép màn, và cả trận vẽ theo thứ tự nào từ nền tới HUD.

## Ý cốt lõi

### layers-four | AI 8 | Nền có đúng bốn lớp: trời, cảnh xa, dải đất, decor
Mỗi lớp một việc: trời làm phông, cảnh xa cho chiều sâu, dải đất là chỗ đứng, decor làm hai bên thành bớt trống.

### sky-above-ground | AI 8 | Trời phủ từ mặt đất lên tới mép trên khung hình
Trời luôn nằm sau mọi lớp khác; phần trời sau dải HUD trên vẫn được vẽ.

### far-on-horizon | AI 7 | Cảnh xa (đồi, núi, rừng) đứng trên đường mặt đất, thấp hơn thành nhiều
Cảnh xa không cao tới dải HUD trên và không lấn sự chú ý khỏi thành, hero, quái.

### ground-below-line | AI 8 | Dải đất phủ từ mặt đất xuống tới mép dưới khung hình, liền một dải qua dưới thành
Dải đất là mặt đứng của thành, hero, quái (`feature-castle/castle-core`); phần dưới bị thanh nâng cấp che vẫn được vẽ.

### layers-full-width | AI 8 | Trời, cảnh xa và dải đất rộng hơn mép màn để không hở ở màn rộng
Mép màn của thành là khung ở màn chuẩn; màn rộng hơn thấy thêm hai bên, nền phải phủ cả phần đó.

### decor-two-sides | AI 7 | Decor (cỏ, đá, cờ, hàng rào) đặt dọc hai bên thành, đối xứng gần đúng
Hai bên trông như nhau để không bên nào trông "nguy" hơn chỉ vì decor. Decor không đặt trùng thân thành.

### draw-order | AI 8 | Cả trận vẽ từ sau ra trước: trời, cảnh xa, dải đất, decor, thành, quái và hero, hiệu ứng, HUD, popup
Nền luôn dưới thành; thành dưới quái và hero để thấy được unit đứng ở mép thành; HUD và popup trên cùng.

### layers-static | AI 7 | Các lớp nền đứng yên trong trận, không đổi theo đợt hay máu thành
Nền không báo tin gì cho người chơi; thông tin trận nằm ở `feature-hud`.
