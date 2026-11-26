---
based-on: ca84fa282d14
node-by: gpt@r2-all-2
by: combat-own-code=gpt@r2-1#combat-co-code-rieng, combat-every-hit=gpt@r2-1#moi-don-qua-giao-chien, combat-same-moment=gpt@r2-1#cung-buoc-mo-phong, attacker-picks=gpt@r2-1#ben-ra-don-chon, attacker-locks-damage=gpt@r2-1#chot-sat-thuong-luc-ra-don, state-before-hp=gpt@r2-1#trang-thai-truoc-khi-tru-mau, sum-per-target=gpt@r2-1#cong-sat-thuong-theo-ben-trung, combat-sides-own-hp=gpt@r2-1#chu-so-huu-tru-mau, owner-reports-once=gpt@r2-1#chu-so-huu-bao-het-mau, combat-report-after=gpt@r2-1#bao-sau-khi-tru-mau-xong, combat-tick-done=gpt@r2-1#moc-da-bao-du-ket-qua
---
# Giao chiến

Nơi phân xử các đòn trúng ra cùng một lúc giữa hero, quái và thành, để kết quả nhất quán dù bên nào được xử lí trước. Mỗi bên vẫn tự chọn mục tiêu, tự tính sát thương và tự trừ máu của mình.

## Ý cốt lõi

### combat-own-code | AI 5 | Giao chiến có code riêng, điều phối việc nhận đòn, phân xử và chốt từng nhịp
Giữ thứ tự xử lí chung giữa hero, quái và thành, để hai đòn kết liễu cùng nhịp đều có hiệu lực.

### combat-every-hit | AI 5 | Mọi đòn trúng đều đi qua giao chiến, kể cả khi nhịp chỉ có một đòn
Cùng một đường cho đòn tự đánh và kĩ năng gây sát thương; quái vẫn tự cập nhật máu.

### combat-same-moment | AI 6 | Các đòn trúng trong cùng một bước của trận được phân xử chung
Mỗi đòn thuộc bước mà nó trúng; hero và quái cùng kết liễu nhau trong một bước thì cả hai đều hết máu.

### attacker-picks | AI 7 | Bên ra đòn tự chọn đối tượng nhắm và xác định ai trúng theo luật của đòn
Đòn chém nhắm một con mà trúng được nhiều con.

### attacker-locks-damage | AI 7 | Bên ra đòn chốt sát thương cho từng bên trúng theo chỉ số lúc ra đòn
Đòn đã ra giữ mức đã chốt nếu nó trúng sau lúc người chơi vừa nâng cấp.

### state-before-hp | AI 6 | Giao chiến xét bên nào nhận được đòn theo trạng thái ngay trước khi trừ máu của nhịp
Hero đã gục hay đang hiện hình khi hồi sinh bỏ qua đòn của nhịp đó; hero còn đánh thì nhận mọi đòn hợp lệ cùng nhịp.

### sum-per-target | AI 6 | Giao chiến cộng sát thương các đòn hợp lệ cùng nhịp thành một tổng cho từng bên trúng
Hero và thành cùng đánh một con thì quái nhận một tổng.

### combat-sides-own-hp | AI 7 | Chỉ feature sở hữu bên trúng mới cập nhật máu của bên đó, một lần theo tổng của nhịp
Giao chiến không giữ máu hay chỉ số; nó chuyển tổng sát thương tới từng bên sở hữu.

### owner-reports-once | AI 7 | Feature sở hữu phát đúng một tin gục, bị hạ hoặc bị phá mỗi khi máu bên mình từ dương về 0
Hai đòn chí tử cùng nhịp tạo một tin; hero hồi sinh rồi gục lần nữa tạo tin mới.

### combat-report-after | AI 6 | Giao chiến chỉ mở bước báo kết quả sau khi mọi bên trúng đã cập nhật máu của nhịp
Thành và quái cuối cùng hết máu thì các bên nhận tin đều thấy trạng thái đã cập nhật đủ; quyền chốt thua vẫn của `feature-run`.

### combat-tick-done | AI 6 | Giao chiến báo nhịp đã xong khi các feature sở hữu đã phát mọi tin hết máu của nhịp
Đợt quái biết đã đủ tin bị hạ để xét dọn sạch; luật thua trước vẫn giữ.
