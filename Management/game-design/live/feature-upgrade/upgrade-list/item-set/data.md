---
based-on: 7294a54c79df
---
# Bộ mục nâng cấp của lượt

Danh sách mục cụ thể khi vào trận: một mục cấp hero cho mỗi hero trong đội 3 hero, cộng ba mục thành, tổng 6 mục. Mọi mục bắt đầu ở cấp 1.

## Ý cốt lõi

### six-items | AI 8 | Mỗi lượt có đúng 6 mục: 3 mục cấp hero và 3 mục thành
Đội có 3 hero nên có 3 mục hero; thành có máu, sức đánh, kĩ năng.

### item-order | AI 8 | Thứ tự mục cố định: hero chỗ 1, hero chỗ 2, hero chỗ 3, máu thành, sức đánh thành, kĩ năng thành
Hero theo đúng thứ tự người chơi xếp lúc chọn đội.

### start-level-1 | AI 8 | Mọi mục vào trận ở cấp 1, chưa mua lần nào
Cấp = 1 + số lần đã mua mục đó trong lượt.

### built-at-run-start | AI 7 | Danh sách 6 mục được lập một lần khi vào trận, từ đội nhận qua feature-run, và không đổi tới hết lượt
Hero gục không làm mất mục; lượt mới lập lại từ đầu.

### item-identity | AI 7 | Mỗi mục được nhận diện bằng chỗ trong đội (hero 1-3) hoặc tên mục thành, không bằng thứ tự nút
Để mục hero luôn trỏ đúng hero kể cả khi hai hero cùng loại.
