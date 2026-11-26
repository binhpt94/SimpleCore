---
based-on: 91a5bf33f442
refs: feature-battlefield/battlefield-layers/layer-values@9b7d55c81fc4, feature-hero/hero-list/hero-roster-values@b91d8c9d12d4, feature-popup/popup-layer/stack-rules@fa85847e35ee, feature-hud/hud-layout/layout-spec@1a7ddf5f3b6c
---
# Nhân vật Spine thử: bộ mẫu, chỗ đứng, animation và nút

Chốt bộ xương mẫu, cỡ và chỗ đứng của nhân vật, tên ba animation, thời gian chuyển mượt, bốn nút thử và nhãn trạng thái. Số nằm trong bảng `Params` của node.

## Ý cốt lõi

### sample-spineboy | AI 7 | Nhân vật là bộ mẫu spineboy đi kèm runtime spine-unity
Bộ mẫu chính thức, có sẵn idle, run, jump. Nếu bản runtime được cài không kèm spineboy thì dùng bộ mẫu người chơi được có đủ một animation đứng yên, một chạy, một hành động, và ghi lại tên.

### char-height | AI 8 | Nhân vật cao 1.5u (90 px trên màn chuẩn), bằng chiều cao hộp của hero
Hộp hero 1u × 1.5u theo `feature-hero/hero-list/hero-roster-values`. Đo theo chiều cao bộ xương ở tư thế idle.

### char-ground | AI 8 | Chân nhân vật chạm mặt đất y=0, không lơ lửng, không lún
Mặt đất theo `feature-battlefield/battlefield-layers/layer-values`. Lúc jump nhân vật rời đất theo animation rồi về đúng y=0.

### char-place | AI 8 | Nhân vật đứng ở x=+9u, cánh phải
Quái bên phải đi qua trước khi tới thành nên thấy được nhân vật đứng chung lớp với quái. Cách xa khối thử ở +5u và đá decor ở +6.5u.

### face-start | AI 7 | Lúc màn trận mở nhân vật quay mặt về phía thành (sang trái)
Giống hướng hero đứng chờ quái ở cánh phải. Nút Lật đổi sang mặt phải và ngược lại.

### anim-names | AI 8 | Ba animation là idle (đứng yên, lặp), run (chạy, lặp), jump (nhảy, một lần)

### mix-time | AI 8 | Mọi lần đổi animation chuyển mượt trong 0.2 giây
Đủ ngắn để không trễ, đủ dài để thấy trộn. Áp cho cả lúc jump chạy xong tự về idle.

### match-time | AI 8 | Animation chạy tốc độ 1 khi trận chạy; trận dừng thì đứng nguyên tư thế, trận chạy lại thì chạy tiếp từ đúng chỗ
Tín hiệu dừng của `feature-run` tới qua nơi lắp màn, nhân vật không gọi thẳng feature khác. Đang nhảy hay đang chuyển mượt lúc dừng cũng đứng nguyên, chạy lại thì làm tiếp phần còn lại.

### same-unit-layer | AI 7 | Nhân vật vẽ ở lớp unit, ngay trên hero và quái cùng lớp, dưới hiệu ứng
Quái đi qua thì nhân vật che lên quái; hiệu ứng đòn vẫn vẽ đè lên nhân vật.

### four-buttons | AI 8 | Có bốn nút: "Đứng", "Chạy", "Nhảy", "Lật"
Ba nút đầu đổi animation; nút Lật quay mặt. Chi tiết tương tác ở ui.md.

### same-anim-press | AI 7 | Bấm nút của animation lặp đang chạy thì không làm gì; bấm "Nhảy" khi đang nhảy thì nhảy lại từ đầu

### flip-keeps-anim | AI 8 | Lật mặt xảy ra ngay, animation đang chạy tiếp tục từ đúng chỗ đang chạy

### state-label-text | AI 7 | Nhãn trạng thái ghi "<tên animation> · <lặp|một lần> · mặt <trái|phải>"
Ví dụ: "run · lặp · mặt trái". Đổi ngay khi bấm nút hay khi jump xong về idle.

### wide-same | AI 8 | Ở màn 21:9 nhân vật vẫn cao 90 px và đứng đúng x=+9u như ở 16:9

### popup-dims | AI 8 | Popup mở thì nền che tối phủ nhân vật, nút thử không bấm được tới khi popup đóng
Theo `feature-popup/popup-layer/stack-rules`.

### touch-lock | AI 7 | Nút thử chỉ nhận chạm khi trận đang chạy và không có popup chặn bấm; chạm lúc bị chặn bị bỏ
Kể cả lúc trận đã dừng mà popup chưa hiện, và lúc popup đã đóng mà trận chưa chạy lại.

### dev-build | AI 8 | Nhân vật, nút và nhãn chỉ có trong Unity Editor và bản Development build
Bản phát hành không có, như mọi thứ chỉ để thử.

### flip-anchor | AI 8 | Lật mặt lấy gốc ở chân nhân vật: chân vẫn ở x=+9u, y=0 trước và sau khi lật

### mid-mix-switch | AI 7 | Đổi animation giữa lúc đang chuyển mượt thì chuyển mượt 0.2 giây từ tư thế đang hiện sang animation mới
Nhảy bị ngắt bởi Chạy hay Đứng thì không còn tự về idle.

### narrow-fits | AI 7 | Ở 16:9, tỉ lệ hẹp nhất, đỉnh cú nhảy và phụ kiện xa nhất của nhân vật nằm trọn trong khung nhìn ở cả hai mặt
16:9 thấy y tới +14u theo `feature-battlefield/battlefield-layers/layer-values`; spineboy cao 1.5u nhảy không tới đó. Mép phải x=+16u cách nhân vật 7u.

### perf-measure | AI 6 | Đo p95 thời gian khung hình của 60 giây trận đông, một lượt bật và một lượt tắt nhân vật thử
Cùng máy, đội, seed và thao tác; ghi p95 hai lượt và phần tăng khi bật. Chưa có ngưỡng đạt vì thiết kế chưa có ngân sách FPS; người xem số rồi quyết.
