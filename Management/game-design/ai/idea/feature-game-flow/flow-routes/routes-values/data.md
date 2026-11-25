---
based-on: d4ab4cd4389f
refs: feature-run/run-flow/flow-rules@f011f018b6c5, feature-team-select/team-memory/memory-rules@e36a9ec522a1
frame: none
---
# Thông số đổi màn

Đổi màn là đổi ngay trong scene Game, khoá yêu cầu mới tới khi màn đích hiện, có nút "Quay lại" ở màn chọn đội, và chỉ có một lần thử lại khi dựng trận lỗi.

## Ý cốt lõi

### switch-instant | AI 7 | Đổi giữa hai màn chính là tức thì: màn cũ ẩn và màn mới hiện trong cùng một khung hình
Riêng vào trận: màn chọn đội ở lại tới khi trận dựng xong rồi mới đổi.

### lock-until-shown | AI 7 | Khoá yêu cầu đổi màn từ lúc nhận yêu cầu tới khung hình màn đích hiện
Yêu cầu tới trong lúc khoá bị bỏ, ghi cảnh báo.

### build-limit | AI 6 | Dựng trận quá 3 giây (dự kiến) hoặc báo lỗi thì ở lại màn chọn đội, hiện dòng "Không vào được trận, thử lại" và mở lại nút "Vào trận"
Đội vừa xác nhận vẫn trong các chỗ; đội được nhớ chưa đổi.

### back-button | AI 6 | Màn chọn đội có nút "Quay lại" ở góc trên trái, cạnh tối thiểu 120 px; chạm thì về màn bắt đầu
Đội được nhớ không đổi; các chỗ đang chọn dở bị bỏ.

### home-first-once | AI 7 | Mỗi lần scene Game sẵn sàng, màn bắt đầu hiện đúng một lần; báo sẵn sàng lặp lại bị bỏ

### back-key-map | AI 7 | Nút quay lại của máy: ở màn chọn đội như "Quay lại"; ở màn bắt đầu không làm gì
Trong màn trận giữ luật của `feature-run` (như bấm tạm dừng).
