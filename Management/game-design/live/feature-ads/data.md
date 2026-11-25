---
based-on: 6181a7c21bf0
by: ads-result=gpt@r2-1#ads-verified-result, ads-game-waits=gpt@r2-1#ads-battle-freeze, ads-revive-consent=gpt@r2-1#ads-revive-consent, ads-result-recipient=gpt@r2-1#ads-result-recipient, ads-reward-owner=gpt@r2-1#ads-reward-owner, ads-return-timing=gpt@r2-1#ads-return-timing, ads-repeat-tap=gpt@r2-1#ads-repeat-tap, ads-pending-text=gpt@r2-1#ads-pending-text, ads-response-timeout=gpt@r2-1#ads-response-timeout, ads-failure-message=gpt@r2-1#ads-failure-message, ads-stale-result=gpt@r2-1#ads-stale-result
---
# Quảng cáo

Nơi duy nhất trong game hiện quảng cáo có thưởng. Feature khác nhờ hiện một quảng cáo và nhận lại kết quả: đã xem hết, hay không xem được.

## Ý cốt lõi

### ads-rewarded-only | AI 7 | Game chỉ có quảng cáo do người chơi tự chọn xem để nhận thưởng
Không có quảng cáo tự bật giữa trận hay giữa các màn. Đã loại: quảng cáo xen màn.

### ads-one-place | AI 7 | Mọi quảng cáo đều đi qua feature này; feature khác không tự gọi nhà quảng cáo
Feature gọi chỉ biết kết quả, không biết quảng cáo lấy từ đâu.

### ads-revive-consent | AI 7 | Popup hồi sinh chỉ gọi quảng cáo khi người chơi bấm Xem trong đề nghị do lượt chơi mở

### ads-result | AI 8 | Mỗi yêu cầu nhận đúng một kết quả, chỉ thành công khi nhà quảng cáo xác nhận đủ điều kiện nhận thưởng
Đóng quảng cáo và đủ điều kiện nhận thưởng là hai việc riêng; đã xác nhận thưởng thì đóng sau đó vẫn là thành công.

### ads-result-recipient | AI 7 | Kết quả của từng yêu cầu trả về đúng feature đã gửi yêu cầu đó
Với hồi sinh, popup hồi sinh nhận kết quả và chuyển cho `feature-run`.

### ads-return-timing | AI 7 | Quảng cáo đã mở thì kết quả chỉ trả về sau khi quảng cáo đóng và game trở lại trên màn hình

### ads-response-timeout | AI 6 | Hết hạn chờ mở quảng cáo, hay hết hạn chờ xác nhận thưởng sau khi đóng, thì báo thất bại
Mất mạng hay thiếu phản hồi vẫn có kết quả cuối; thời gian quảng cáo đang phát không tính vào hạn chờ.

### ads-stale-result | AI 7 | Kết quả chỉ có hiệu lực với đúng đề nghị hồi sinh còn đang chờ
Kết quả tới sau khi đề nghị đã xử lí hay lượt đã rời trận thì bị bỏ qua, không hồi sinh ở lượt sau.

### ads-reward-owner | AI 7 | Chỉ lượt chơi được ra lệnh thành hồi sinh sau khi nhận kết quả xem thành công
Lượt chơi giữ quyền hồi sinh một lần mỗi lượt; `feature-castle` thực hiện và cập nhật máu.

### ads-game-waits | AI 7 | Lượt chơi giữ trận đứng yên và khoá thao tác chiến đấu suốt luồng hồi sinh
Quái, tiến độ đợt và hồi chiêu giữ nguyên từ lúc mở đề nghị; xem thành công thì trận chỉ chạy tiếp sau khi thành đã hồi sinh.

### ads-repeat-tap | AI 7 | Mỗi đề nghị hồi sinh chỉ gửi một yêu cầu xem quảng cáo
Các lần bấm Xem tiếp theo của cùng đề nghị bị bỏ qua.

### ads-pending-text | AI 6 | Popup hồi sinh hiện trạng thái đang chờ quảng cáo khi chưa có kết quả

### ads-failure-message | AI 6 | Popup thua hiện dòng "Quảng cáo chưa hoàn tất" khi xem để hồi sinh thất bại
Hết quảng cáo, mất mạng và đóng sớm đều theo luồng thua; dòng này nằm trong `feature-popup-lose`.
