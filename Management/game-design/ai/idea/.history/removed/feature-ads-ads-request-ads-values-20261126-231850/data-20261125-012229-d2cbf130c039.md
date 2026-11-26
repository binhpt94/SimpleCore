---
based-on: fb0adae32557
frame: none
---
# Thông số yêu cầu quảng cáo

Hạn chờ của từng bước, kết quả khi lỗi, và cách chạy thử khi chưa có nhà cung cấp thật.

## Ý cốt lõi

### open-limit | AI 7 | Hạn chờ mở quảng cáo là 8 giây (dự kiến); quá hạn là "không xem được"
Đủ cho mạng chậm tải một quảng cáo; người chơi đang nhìn popup chờ nên không để lâu hơn.

### confirm-limit | AI 7 | Hạn chờ xác nhận thưởng sau khi quảng cáo đóng là 5 giây (dự kiến); quá hạn là "không xem được"

### return-wait | AI 6 | Kết quả trả về ở khung hình đầu tiên sau khi game trở lại màn hình
Game chưa trở lại (người chơi còn ở ngoài) thì kết quả chờ, không có hạn.

### busy-reply | AI 7 | Yêu cầu chen vào khi đang có yêu cầu nhận "không xem được" ngay trong khung hình đó

### test-provider | AI 7 | Bản dev có nhà cung cấp giả: chọn trước kết quả "đã xem đủ", "không xem được" hay "quá hạn" để thử luồng hồi sinh
Nhà cung cấp giả hiện một màn chờ 2 giây thay cho quảng cáo; chỉ chạy ở bản dev.

### log-reason | AI 6 | Mỗi kết quả "không xem được" ghi lí do vào log (hết quảng cáo, mất mạng, đóng sớm, quá hạn, đang bận)
Người chơi không thấy lí do; log để phiên code và người dò lỗi.
