---
based-on: 469d9009792c
---
# Cân bằng số liệu

Nhìn mọi số liệu của game (sức hero, sức quái, độ khó đợt, giá nâng cấp, tiền nhận) như một tổng thể, để lượt chơi khó dần đúng nhịp và lượt nào rồi cũng kết thúc.

## Ý cốt lõi

### balance-curve | AI 7 | Quái mạnh lên nhanh hơn sức mạnh mua được, nên lượt nào rồi cũng thua
Chơi vô tận cần một điểm gãy; người chơi giỏi chọn đội và chi tiền khéo thì gãy muộn hơn.

### balance-tunable | AI 7 | Chỉnh được số liệu mà không sửa code
Mỗi feature giữ số của mình trong cấu hình riêng; feature này quyết các số đó cùng nhau.

### balance-team-fair | AI 6 | Không có đội hero nào luôn thắng mọi đội khác
Để màn chọn đội thật sự là một lựa chọn.
