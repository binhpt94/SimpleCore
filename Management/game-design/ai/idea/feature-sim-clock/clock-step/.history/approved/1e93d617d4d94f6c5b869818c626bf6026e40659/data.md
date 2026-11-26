---
based-on: 83ee7a4052d1
refs: glossary#sim-step@ea2316690640, feature-combat/combat-tick/tick-values@aa74e1a2c37b
---
# Bước nhịp và cách chạy

Nhịp chung chia trận thành các bước nhịp. Mỗi bước, các feature gameplay cập nhật một lần theo thứ tự cố định. Config chung chọn chạy theo bước cố định hay theo khung hình.

## Ý cốt lõi

### one-update-per-step | AI 7 | Mỗi bước nhịp, mỗi feature gameplay cập nhật đúng một lần với cùng một khoảng thời gian bước
Feature không tự đo thời gian; khoảng thời gian do nhịp đưa.

### two-modes | AI 7 | Có hai cách chạy: bước cố định (TickUpdate) và theo khung hình (Update); config chung chọn một
Ở cách bước cố định, bước nào cũng dài bằng nhau. Ở cách theo khung hình, mỗi khung là một bước dài bằng thời gian khung.

### catch-up-cap | AI 7 | Khung hình dài thì chạy nhiều bước trong một khung, có trần; vượt trần thì bỏ phần dư, trận chậm lại chứ không nhảy
Ở cách theo khung hình, thời gian bước cũng bị kẹp theo trần đó.

### fixed-order | AI 7 | Thứ tự các feature trong một bước nhịp cố định và ghi rõ
Cùng thứ tự thì cùng kết quả. Đợt quái thả quái trước, các bên ra đòn sau, giao chiến phân xử sau các bên ra đòn, lượt xét thua sau cùng.

### input-on-step | AI 7 | Thao tác của người chơi được áp vào đầu bước nhịp kế tiếp và ghi theo số bước
Bấm kĩ năng, mua nâng cấp. Ghi theo số bước thì chơi lại đúng chuỗi thao tác cho cùng kết quả.

### input-queue | AI 7 | Nhịp giữ hàng chờ thao tác chung, áp ở đầu bước kế tiếp đúng thứ tự đã đưa vào
Một nơi duy nhất giữ bản ghi thao tác theo số bước. Bên dùng: mua nâng cấp, bấm kĩ năng thành (danh sách ở `CLAUDE.md`).

### input-while-paused | AI 7 | Thao tác đưa vào lúc trận dừng vẫn áp ở bước kế tiếp; feature nhận tự từ chối nếu luật của nó cấm khi trận dừng
Ví dụ mua nâng cấp sau khi thành bị phá bị `feature-upgrade` từ chối.

### clock-time | AI 7 | Mọi chờ giờ của gameplay tính bằng đồng hồ gameplay của nhịp, không theo giờ thật
Hồi chiêu, hồi sinh, lúc thở, lịch thả quái, chờ trước popup thua đều đếm theo các bước mà lượt cho trận chạy.

### view-reads-latest | AI 7 | Màn hình vẽ theo khung hình từ trạng thái mới nhất của gameplay; phần nhìn không chạy luật
Phần nhìn chỉ đọc, không đổi trạng thái gameplay.

### pause-keeps-clock | AI 7 | Lúc trận dừng bước nhịp vẫn chạy để áp hàng chờ, còn đồng hồ gameplay đứng yên
Feature tự đứng yên theo trạng thái trận của `feature-run`; nhịp không dùng cách làm chậm thời gian của engine.

### dev-fast-forward | AI 7 | Bản dev cho tester tua nhanh: tăng số bước mỗi khung, chạy ngay nhiều bước, đóng băng, bước từng bước
Bản phát hành không có tua.
