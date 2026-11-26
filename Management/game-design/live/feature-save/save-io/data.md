---
based-on: 028d6c6cb4ed
node-by: gpt@r2-1
---
# Đọc, ghi và khôi phục dữ liệu lưu

Dữ liệu lưu gồm những gì, đọc trả về gì, ghi diễn ra thế nào, và phải làm gì khi dữ liệu thiếu, hỏng hay khác phiên bản.

## Ý cốt lõi

### io-one-record | AI 8 | Cả game có đúng một bản ghi lưu, gồm số phiên bản dữ liệu và kỉ lục
Không tách bản ghi theo đội hay theo lượt.

### io-read-two-results | AI 8 | Đọc trả về một trong hai kết quả: có kỉ lục, hoặc chưa có
Chưa có gồm: chưa từng ghi, nội dung hỏng, phiên bản lạ. Lượt chơi xử lí ba trường hợp này như nhau.

### io-read-before-home | AI 8 | Đọc lúc mở game xong trước khi màn bắt đầu hiện
Đọc một lần lúc mở game; sau đó lượt chơi giữ kỉ lục trong bộ nhớ, không đọc lại mỗi lần hiện màn.

### io-write-replaces | AI 8 | Ghi thay cả bản ghi cũ bằng bản ghi mới mang số được giao
Lưu dữ liệu không so lớn nhỏ; việc đó của `feature-run`.

### io-write-reports | AI 7 | Mỗi lần ghi kết thúc bằng một kết quả: thành công hoặc thất bại

### io-write-whole | AI 7 | Bản ghi mới chỉ thay bản cũ khi đã ghi đủ; ghi dở thì bản cũ còn nguyên

### io-write-last-wins | AI 7 | Nhiều lần ghi liền nhau được làm theo thứ tự nhận; lần nhận sau cùng là bản còn lại

### io-older-version | AI 6 | Bản ghi của phiên bản dữ liệu cũ hơn được đọc theo dạng cũ rồi trả về kỉ lục như thường

### io-newer-version | AI 6 | Bản ghi của phiên bản dữ liệu mới hơn thì đọc báo chưa có, và mọi lần ghi bị từ chối (báo thất bại)
Mở bằng bản game cũ không phá dữ liệu của bản mới; lượt chơi vẫn chơi bình thường với kỉ lục trong bộ nhớ.

### io-broken-not-erased | AI 6 | Nội dung hỏng thì đọc báo chưa có, Lưu dữ liệu không tự xoá hay sửa bản ghi đó
Bản ghi chỉ bị thay khi lượt chơi giao ghi một kỉ lục mới.
