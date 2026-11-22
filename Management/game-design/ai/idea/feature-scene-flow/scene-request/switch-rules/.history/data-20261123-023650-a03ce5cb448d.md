---
based-on: 646c23aa767e
---
# Các bước và luật của một lần chuyển

Một lần chuyển đi qua sáu bước cố định với lớp che bật tắt tức thì. Yêu cầu sai (tên scene lạ, đang chuyển, chuyển sang chính scene đang chạy) bị từ chối rõ ràng; lỗi giữa chừng thì lớp che giữ nguyên và báo lỗi.

## Ý cốt lõi

### switch-steps | AI 8 | Một lần chuyển đi đúng sáu bước: nhận tên, hiện lớp che, huỷ scene cũ, tải scene mới, chạy scene mới, bỏ lớp che
Bước sau chỉ bắt đầu khi bước trước xong. Lớp che bỏ đi sau khi scene mới đã tạo xong view của bước chạy, nên người chơi không thấy màn trống.

### cover-instant | AI 7 | Lớp che hiện và biến mất tức thì, che kín toàn màn hình và chặn mọi lần bấm
Lớp che không trong suốt. Nội dung lớp che giống màn Loading: tên game và thanh tiến độ tải của scene mới.

### reject-unknown | AI 7 | Xin chuyển tới tên scene không có trong danh sách thì từ chối, ghi lỗi nêu tên đó, scene hiện tại chạy tiếp
Danh sách scene: Loading, Game (`feature-scene-flow/scene-list`).

### reject-busy | AI 7 | Đang chuyển mà có yêu cầu chuyển mới thì yêu cầu mới bị bỏ qua và ghi cảnh báo
Không xếp hàng chờ. Đã loại: hàng chờ yêu cầu (dễ chuyển hai lần liền ngoài ý muốn).

### reject-same | AI 6 | Xin chuyển sang chính scene đang chạy thì bỏ qua, không tải lại
Đặt lại lượt chơi không đi qua đây (`feature-session-scene/session-clean`).

### switch-fail | AI 7 | Tải hoặc chạy scene mới bị lỗi thì lớp che giữ nguyên, hiện dòng lỗi nêu tên feature lỗi, không quay về scene cũ
Scene cũ đã huỷ nên không quay về được. Người chơi tắt mở lại game.
