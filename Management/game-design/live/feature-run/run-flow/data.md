---
based-on: d8a90fd4adc0
---
# Luồng màn hình của lượt

Thứ tự các màn trong một lượt, màn nào dẫn sang màn nào, lượt kết thúc lúc nào, và các màn nằm ở scene nào.

## Ý cốt lõi

### flow-order | AI 8 | Một lượt đi đúng một đường: màn bắt đầu, màn chọn đội, trận, popup thua
Mở game là tới màn bắt đầu (`feature-home`); bấm chơi sang màn chọn đội (`feature-team-select`); đủ đội bấm vào trận; thua thì hiện popup thua (`feature-popup-lose`). Không có lối tắt bỏ qua màn chọn đội. Đã loại: vào thẳng trận khi bấm chơi.

### flow-end-trigger | AI 8 | Lượt kết thúc khi thành báo bị phá; lúc đó cả trận dừng lại
Chỉ `feature-castle` báo thua; feature này nhận báo, dừng trận (quái thôi tới, hero và quái thôi đánh, không mua nâng cấp, không bấm kĩ năng) rồi mới hiện popup thua. Đã loại: trận vẫn chạy phía sau popup thua.

### flow-after-lose | AI 8 | Từ popup thua có hai lối: chơi lại về màn chọn đội, hoặc về màn bắt đầu
Chơi lại không đi qua màn bắt đầu. Đã loại: chơi lại vào thẳng trận với đội cũ (người chơi mất dịp đổi đội).

### flow-quit | AI 7 | Bỏ lượt từ popup tạm dừng về thẳng màn bắt đầu
Bỏ lượt kết thúc lượt giống như thua về mặt điểm (`feature-run/run-score`), nhưng không hiện popup thua. Đã loại: bỏ lượt rồi hiện popup thua (người chơi đã chủ động rời, xem kết quả là thừa).

### flow-scenes | AI 6 | Màn bắt đầu và màn chọn đội chung một scene menu; trận và hai popup của trận ở scene trận
Mỗi lần vào trận là tải mới scene trận, nên lượt mới luôn bắt đầu sạch (`feature-session-scene`). Chuyển scene đều xin qua `feature-scene-flow`. Đã loại: mọi màn chung một scene (phải tự dọn trận cũ bằng tay); mỗi màn một scene (chuyển nhiều, chậm).
