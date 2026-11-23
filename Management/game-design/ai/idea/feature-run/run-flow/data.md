---
based-on: d8a90fd4adc0
refs: feature-run/run-score@6f1909d90f98, feature-scene-flow/scene-list@78ed2dcc0859, feature-run/run-start@31cb7598b529, glossary#run@721a2b6e316e
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

### flow-scenes | AI 7 | Mở game qua scene Loading rồi vào màn bắt đầu; màn bắt đầu, chọn đội, trận và hai popup đều trong scene Game
Chuyển màn là đổi trong scene Game, không chuyển scene; chơi lại không tải lại scene. Lượt mới vẫn sạch vì trận được dựng mới và huỷ hết mỗi lần. Theo `feature-scene-flow/scene-list`. Đã loại: scene menu và scene trận riêng (thêm chờ tải giữa các lượt); mỗi màn một scene.
