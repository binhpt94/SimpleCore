---
based-on: 95aa661c0148
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

### dev-build-only | AI 8 | Nhân vật, nút và nhãn thử chỉ có ở bản dev; bản cho người chơi không có chúng
Hiện thực `dev-only` của feature.

### all-required | AI 7 | Mọi điều kiểm của node này là bắt buộc để lần thử đạt, trừ độ hợp style ghi riêng
Hiện thực `pass-rule`: độ hợp style (`style-fit`) không làm lần thử trượt, chỉ là đầu vào cho quyết định làm art Spine riêng.

### pause-freeze | AI 7 | Trận dừng thì nhân vật đứng nguyên tư thế; trận chạy lại thì animation chạy tiếp từ đúng chỗ
Kể cả lúc đang nhảy và lúc đang chuyển mượt. Tín hiệu dừng của `feature-run` tới qua nơi lắp màn.

### touch-lock | AI 7 | Nút thử chỉ nhận chạm khi trận đang chạy và không có popup chặn bấm
Chạm lúc bị chặn bị bỏ, không thành lệnh khi trận chạy lại.

### overlap-order | AI 8 | Hero hay quái đi ngang qua thì mọi phần của nhân vật giữ cùng thứ tự vẽ so với sprite đó, suốt clip
Thân, tay, phụ kiện không phần nào lọt ra sau sprite hay đổi lớp giữa chừng.

### flip-anchor | AI 8 | Lật mặt giữ nguyên chỗ chân trên mặt đất, không trượt ngang, không nhấc lên

### mid-mix-switch | AI 7 | Đổi animation giữa lúc đang chuyển mượt thì chuyển tiếp từ tư thế đang hiện sang animation mới
Lần nhảy bị ngắt không tự kéo nhân vật về đứng yên khi đã sang animation khác.

### size-vs-hero | AI 7 | Nhân vật ở tư thế đứng cao bằng hộp hero; ghi thêm chênh với chiều cao nhìn thấy của sprite hero

### narrow-camera | AI 7 | Ở tỉ lệ màn hẹp nhất được hỗ trợ, camera giữ trọn hình nhân vật qua mọi animation và cả hai mặt
Đo tới đỉnh cú nhảy và phụ kiện xa nhất. Bổ sung cho `wide-screen`.

### material-check | AI 7 | Màu và mép trong suốt của nhân vật đúng trên cả vùng sáng và vùng tối của nền trận, ở cả hai mặt
Không mảng hồng, ô đục, quầng viền, không mất chi tiết khi lật.

### style-fit | AI 7 | Hình bóng, viền và bảng màu của nhân vật được so với sprite hero ở cỡ trong trận
Hiện thực `art-fit`; ghi riêng, không làm lần thử trượt.

### perf-cost | AI 6 | Đo thời gian xử lý khung hình của một đoạn trận đông khi bật và khi tắt nhân vật thử, cùng máy và cùng cảnh
Ghi phần tăng khi bật. Ngưỡng đạt theo ngân sách FPS khi thiết kế có ngân sách đó.
