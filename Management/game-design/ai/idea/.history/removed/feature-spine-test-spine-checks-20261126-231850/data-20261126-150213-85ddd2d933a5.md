---
based-on: 5033d18c216a
refs: feature-battlefield/battlefield-layers@5c2132462329
---
# Nhân vật thử và các điều kiểm về Spine

Một nhân vật Spine đứng trên mặt đất của trận, với ba animation: đứng yên lặp, chạy lặp, một hành động chạy một lần. Người thử bấm nút để đổi animation và quay mặt, một nhãn ghi animation đang chạy.

## Ý cốt lõi

### one-character | AI 8 | Có đúng một nhân vật Spine thử trên chiến trường
Mọi điều cần kiểm đều thấy được trên một nhân vật bằng cách đổi animation và quay mặt.

### unit-layer | AI 8 | Nhân vật vẽ ở lớp của quái và hero, như một hero thật
Theo thứ tự vẽ ở `feature-battlefield/battlefield-layers`: trước thành, sau hiệu ứng. Thấy được Spine đứng chung lớp với sprite của unit có lỗi thứ tự không.

### same-camera | AI 8 | Nhân vật dùng chung camera với sprite 2D, chân đặt trên mặt đất như hero và quái
Không có camera riêng, để thấy đúng cỡ và chỗ như khi dùng Spine cho unit thật.

### three-anims | AI 8 | Có ba animation để thử: đứng yên, chạy, một hành động
Đứng yên và chạy là hai animation lặp; hành động (nhảy hoặc đánh) là animation chạy một lần.

### loop-plays-on | AI 8 | Animation lặp chạy mãi, nối vòng không giật, tới khi người thử đổi animation khác

### once-then-idle | AI 8 | Animation một lần chạy hết rồi tự về đứng yên
Thử đúng kiểu đòn đánh của hero: làm một lần rồi về tư thế chờ.

### smooth-switch | AI 8 | Đổi animation thì chuyển mượt từ tư thế đang có, không nhảy khung
Chỗ Spine hay lỗi nhất khi dùng cho unit. Đổi giữa chừng một animation cũng phải mượt.

### run-in-place | AI 7 | Animation chạy diễn tại chỗ, nhân vật không rời chỗ đứng
Chỉ thử hình animation, không thử di chuyển; nhân vật không đi lên đè thứ khác.

### anim-buttons | AI 8 | Mỗi animation có một nút riêng; bấm là đổi sang animation đó
Bấm lại nút của animation một lần đang chạy thì nó chạy lại từ đầu.

### flip-button | AI 8 | Có một nút quay mặt nhân vật sang trái hoặc phải; animation đang chạy giữ nguyên
Unit thật đi cả hai chiều vì quái tới từ hai cánh; thử lật hình Spine có đúng không.

### state-label | AI 7 | Một nhãn ghi animation đang chạy, lặp hay một lần, và mặt đang quay
Ảnh chụp và lần chơi thử đọc được ngay nhân vật đang ở trạng thái nào.

### wide-screen | AI 8 | Ở màn rộng nhân vật giữ nguyên hình, cỡ và chỗ trên chiến trường

### under-hud-popup | AI 8 | HUD và popup vẽ trên nhân vật; nền che của popup làm tối nhân vật như mọi thứ của trận

### shown-with-battle | AI 7 | Nhân vật và các nút thử hiện suốt màn trận và ẩn theo màn trận
