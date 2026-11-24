---
based-on: 0b01673e7a72
---
# Một yêu cầu quảng cáo và kết quả của nó

Một yêu cầu quảng cáo đi qua các bước chờ mở, đang phát, chờ xác nhận thưởng, rồi trả đúng một kết quả cho bên gọi. Feature này không biết thưởng là gì; bên gọi tự xử lí thưởng.

## Ý cốt lõi

### one-request-at-a-time | AI 7 | Mỗi lúc chỉ có một yêu cầu quảng cáo đang chạy
Yêu cầu mới tới khi đang có yêu cầu thì nhận ngay kết quả "không xem được", yêu cầu đang chạy không bị ảnh hưởng.

### request-tagged | AI 7 | Mỗi yêu cầu mang mã của lần đề nghị đã sinh ra nó; kết quả trả về kèm đúng mã đó
Bên gọi dùng mã để biết kết quả có còn thuộc đề nghị đang chờ không.

### result-two-values | AI 8 | Kết quả chỉ có hai giá trị: đã xem đủ để nhận thưởng, hoặc không xem được
Hết quảng cáo, mất mạng, đóng sớm, quá hạn chờ đều là "không xem được"; bên gọi không phải phân biệt.

### request-steps | AI 7 | Một yêu cầu đi qua bốn bước: chờ mở, đang phát, chờ xác nhận thưởng, có kết quả
Mỗi bước chỉ đi tới, không quay lại.

### wait-limits | AI 6 | Bước chờ mở và bước chờ xác nhận thưởng mỗi bước có hạn chờ riêng; quá hạn là "không xem được"
Thời gian quảng cáo đang phát không tính vào hạn chờ.

### result-after-return | AI 7 | Kết quả chỉ trả về sau khi quảng cáo đã đóng và game đã trở lại màn hình
Bên gọi nhận kết quả lúc người chơi đã nhìn thấy game.

### caller-cancel | AI 7 | Bên gọi huỷ yêu cầu (rời trận, đề nghị đã xử lí) thì kết quả tới sau bị bỏ, không trả cho ai
Không có thưởng rơi vào lượt sau.

### no-reward-logic | AI 7 | Feature này không biết thưởng là gì và không ra lệnh thưởng
Hồi sinh thành do `feature-run` ra lệnh sau khi popup hồi sinh nhận kết quả.
