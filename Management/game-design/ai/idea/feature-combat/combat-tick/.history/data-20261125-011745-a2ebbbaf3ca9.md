---
based-on: 728b65c526fa
refs: glossary#target@f51ab44b30ff
---
# Một nhịp giao chiến

Mỗi bước cập nhật của trận là một nhịp giao chiến: gom các đòn trúng, bỏ đòn vào bên không còn nhận đòn, cộng sát thương theo bên trúng, để bên sở hữu trừ máu một lần, rồi mới báo kết quả.

## Ý cốt lõi

### hit-record | AI 7 | Một đòn trúng gồm: bên ra đòn, bên trúng, sát thương đã chốt lúc ra đòn
Bên ra đòn tự chọn ai trúng (một con hay nhiều con) theo luật đòn của mình.

### what-is-a-hit | AI 7 | Đòn tự đánh của hero, quái, thành và kĩ năng gây sát thương (Sấm sét) đều là đòn trúng
Băng giá và Cổ vũ không gây sát thương nên không đi qua giao chiến.

### tick-gather | AI 6 | Mọi đòn trúng trong cùng một bước cập nhật của trận được gom chung một nhịp

### tick-filter | AI 6 | Đòn vào bên đã gục, đang hiện hình khi hồi sinh, đã bị hạ hay đã bị phá ở đầu nhịp thì bị bỏ

### tick-sum | AI 6 | Các đòn còn lại được cộng thành một tổng cho từng bên trúng

### owner-applies | AI 7 | Feature sở hữu bên trúng trừ máu một lần theo tổng, rồi tự báo gục, bị hạ hay bị phá đúng một lần
Hero, quái, thành mỗi bên lo máu của mình.

### tick-done | AI 6 | Khi mọi bên đã trừ máu và báo xong, giao chiến báo nhịp đã xong
Đợt quái xét dọn sạch, lượt chơi xét thành bị phá sau tin này; thành bị phá xét trước dọn sạch.

### no-extra-stats | AI 7 | Giao chiến không có giáp, né, chí mạng; sát thương nhận đúng bằng tổng đã cộng
