---
based-on: 3a45c1ce82c1
refs: feature-battlefield/battlefield-layers/layer-values@9b7d55c81fc4, feature-castle/castle-core/castle-body-values@b648ec300023, feature-hero/hero-list/hero-roster-values@b91d8c9d12d4, feature-enemy/enemy-behavior/enemy-move-values@820f944e332f, feature-popup/popup-layer/stack-rules@fa85847e35ee
---
# Ba khối thử: cỡ, chỗ, góc và màu

Chốt cỡ, chỗ đứng, góc nghiêng, tốc độ xoay, bóng, ánh sáng và màu của ba khối thử trên chiến trường. Số nằm trong bảng `Params` của node.

## Ý cốt lõi

### cube-edge | AI 8 | Mỗi khối là hình lập phương cạnh 1u (60 px trên màn chuẩn)
Rộng bằng hero (hộp 1u × 1.5u ở `feature-hero/hero-list/hero-roster-values`), thấp hơn hero, nên đặt cạnh nhau là so được cỡ.

### cube-ground | AI 8 | Điểm thấp nhất của khối chạm mặt đất y=0, khối không lơ lửng, không lún
Mặt đất theo `feature-battlefield/battlefield-layers/layer-values`.

### cube-back-place | AI 8 | Khối sau thành có tâm ở x=+1.5u, đúng mép phải của thành, nên thành che nửa trái của khối
Mép thành theo `feature-castle/castle-core/castle-body-values`. Hero chỗ 2 xuất phát cũng ở x=+1.5u và đứng trước khối.

### cube-mid-place | AI 8 | Khối giữa thành và hero, quái có tâm ở x=−5u, cánh trái
Quái bên trái sinh ở x=−17u đi sang phải (`feature-enemy/enemy-behavior/enemy-move-values`), nên đi ngang qua khối trước khi tới thành. Nằm giữa cỏ ở −4u và đá ở −6.5u, không chạm decor.

### cube-front-place | AI 8 | Khối trước hero, quái có tâm ở x=+5u, cánh phải
Đối xứng với khối giữa, để hai cánh cho thấy hai thứ tự vẽ khác nhau cùng lúc.

### cube-tilt | AI 8 | Khối nghiêng 20° cho mặt trên ngả về phía camera, và quay sẵn 45° quanh trục đứng
Nhìn thấy mặt trên và hai mặt bên bằng nhau ngay lúc màn trận mở.

### cube-spin | AI 6 | Khối xoay đều 30° mỗi giây quanh trục đứng, theo thời gian thực
Một vòng 12 giây: đủ chậm để không rối mắt. Khối không đọc nhịp trận nên vẫn xoay khi trận đứng yên.

### cube-light | AI 7 | Một đèn hướng chiếu từ trên xuống, nghiêng 30° về phía camera; mặt trên sáng nhất
Đèn chỉ chiếu khối thử, không đổi gì của sprite. Màu nhắm tới: mặt trên gần màu vùng sáng, mặt bên gần màu thân và màu bóng của Vàng trong bảng màu chung.

### cube-shadow | AI 7 | Bóng dưới khối là hình bầu dục phẳng rộng 1.2u, cao 0.25u, tâm ở chân khối trên mặt đất, độ đục 30% (dự kiến)
Màu bóng là Chữ tối của bảng màu chung. Bóng không xoay theo khối.

### cube-color | AI 7 | Khối màu Vàng của bảng màu chung, viền ngoài màu viền của Vàng dày 3 px trên màn chuẩn (dự kiến)
Vàng khác hẳn tông của phe quái và nổi trên nền xanh. Cạnh giữa các mặt không có nét riêng; chỉ đường bao ngoài có viền.

### cube-wide | AI 8 | Ở màn 21:9 khối vẫn cạnh 60 px và đứng đúng toạ độ u như ở 16:9
Màn giữ chiều cao 18u (`feature-battlefield/battlefield-layers/layer-values`), nên rộng ra chỉ thấy thêm chiến trường.

### cube-under-popup | AI 8 | Popup mở thì nền che tối 60% phủ cả ba khối; khối không bao giờ vẽ trên HUD hay popup
Độ tối theo `feature-popup/popup-layer/stack-rules`. Khối cao tới khoảng 1.6u, nằm dưới mép dưới HUD trên (11.7u) và trên mép trên thanh dưới (−1.3u) nên không chạm HUD.
