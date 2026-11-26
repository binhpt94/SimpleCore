---
based-on: ddff8d7773fa
refs: feature-battlefield/battlefield-layers@5c2132462329
---
# Các khối thử và trường hợp hiển thị

Ba khối lập phương 3D đứng trên mặt đất của trận, mỗi khối chen vào một chỗ khác trong thứ tự vẽ. Cả ba cho xem cùng lúc ánh sáng, bóng, viền, màu, cỡ màn và việc nằm dưới HUD, popup.

## Ý cốt lõi

### three-cubes | AI 8 | Có ba khối thử, mỗi khối đứng ở một chỗ khác trong thứ tự vẽ của trận
Một màn trận là thấy cả ba trường hợp thứ tự vẽ, không phải bật tắt. Thứ tự gốc theo `feature-battlefield/battlefield-layers`.

### behind-castle | AI 8 | Một khối vẽ sau thành và trước decor, đứng chồng một phần lên mép thành
Thấy được thành che khối 3D có gọn không.

### between-castle-units | AI 8 | Một khối vẽ trước thành nhưng sau hero và quái, đứng trên đường quái đi qua
Quái và hero đi qua thì che lên khối.

### front-of-units | AI 7 | Một khối vẽ trước hero và quái nhưng sau hiệu ứng, đứng trên đường quái đi qua
Khối che lên quái và hero đi qua; hiệu ứng đòn và kĩ năng vẫn vẽ đè lên khối.

### same-camera | AI 8 | Khối dùng chung camera với sprite 2D, chân khối đặt trên mặt đất như hero và quái
Không có camera riêng cho 3D, để thấy đúng cỡ và chỗ như khi trộn thật.

### tilted-faces | AI 8 | Khối đặt nghiêng để thấy cả mặt trên và hai mặt bên
Nhìn thẳng từ bên cạnh thì khối chỉ là một hình vuông, không thấy gì là 3D.

### slow-spin | AI 6 | Khối tự xoay chậm quanh trục đứng
Thấy sáng tối trên các mặt đổi theo góc. Đã loại: khối đứng yên (chỉ thấy một góc sáng).

### light-from-top | AI 7 | Khối sáng từ trên như style của trận: mặt trên sáng nhất, các mặt bên tối hơn
Đúng luật ánh sáng của `world-cartoon` và của vòng 1.

### ground-shadow | AI 7 | Dưới chân khối có bóng đổ bầu dục tối, như dưới chân hero và quái
Khối không đổ bóng thật theo đèn; bóng là một hình phẳng để giống cách sprite đứng trên đất.

### outline-palette | AI 7 | Màu thân, sáng, tối và viền của khối lấy từ bảng màu chung
Viền là sắc đậm của màu thân, như luật viền của vòng 1, để so với viền sprite.

### wide-screen | AI 8 | Ở màn 16:9 và 21:9 khối giữ nguyên hình, cỡ và chỗ trên chiến trường
Màn rộng chỉ thấy thêm chiến trường hai bên, khối không méo, không giãn.

### under-hud-popup | AI 8 | HUD và popup vẽ trên khối; nền che của popup làm tối khối như mọi thứ của trận
Khối không lòi lên trên HUD hay popup.

### shown-with-battle | AI 7 | Khối hiện suốt màn trận và ẩn theo màn trận
