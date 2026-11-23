---
based-on: db634f5dac0b
refs: feature-run/run-start@31cb7598b529, feature-run/run-score@6f1909d90f98, feature-scene-flow/scene-list@78ed2dcc0859, feature-team-select/team-memory/memory-rules@e36a9ec522a1, feature-run/run-pause@f8e9d49f5079, feature-popup-pause/pause-popup/pause-popup-spec@558a0d06d13f
---
# Luật chuyển màn của lượt

Các bước chuyển màn trong scene Game, thời điểm dừng trận khi thua, độ trễ trước popup thua, và cách chặn bấm trong lúc chuyển màn.

## Ý cốt lõi

### boot-to-home | AI 8 | Mở game: scene Loading tải xong tự sang scene Game, màn đầu tiên là màn bắt đầu
Không có nút ở Loading. Đã vào scene Game thì không quay lại Loading; mọi đổi màn sau đó là đổi trong scene Game, không tải scene.

### fresh-battle | AI 8 | Mỗi lần bấm vào trận, trận được dựng mới hoàn toàn; không giữ gì của trận trước
Thành, hero, quái, HUD, thanh nâng cấp của trận trước bị huỷ hết khi rời trận. Vào trận là dựng lại từ đầu rồi báo "lượt mới" (`feature-run/run-start`).

### lose-stop-same-moment | AI 8 | Thành báo bị phá thì trong cùng khung hình trận dừng hẳn và điểm được chốt
Quái thôi ra, mọi unit thôi đi và thôi đánh, nút kĩ năng và thanh nâng cấp thôi nhận bấm, nút tạm dừng thôi tác dụng. Báo dọn sạch đợt tới sau lúc này bị bỏ qua.

### lose-popup-delay | AI 7 | Popup thua hiện sau 1 giây kể từ lúc trận dừng
1 giây (dự kiến) để người chơi thấy thành vừa bị phá rồi mới đọc kết quả. Trong 1 giây này không có gì bấm được.

### replay-path | AI 8 | Chơi lại ở popup thua: huỷ trận, sang màn chọn đội; về màn bắt đầu: huỷ trận, sang màn bắt đầu
Chơi lại không đi qua màn bắt đầu. Không có hồi sinh.

### quit-path | AI 7 | Đồng ý bỏ lượt: chốt điểm như thua, huỷ trận, về thẳng màn bắt đầu, không hiện popup thua
Kỉ lục cập nhật như thua; màn bắt đầu hiện kỉ lục đã cập nhật.

### transition-lock | AI 7 | Trong lúc đổi màn chỉ lần bấm đầu tiên có tác dụng
Bấm thêm nút ở màn cũ trong lúc đang đổi màn thì bỏ qua, để không mở hai trận hay chuyển màn hai lần.
