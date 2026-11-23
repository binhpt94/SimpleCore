---
based-on: b7789c127bb4
refs: feature-wave/spawning/spawn-order@d71ff27687b4, feature-wave/wave-gap/gap-timer@6bd8e5861fbd
---
# Luật dọn sạch và thưởng đợt

Đợt w dọn sạch đúng lúc con quái cuối cùng của đợt bị hạ sau khi đã ra hết. Ngay lúc đó cộng 20 + 5×w xu, điểm của lượt tăng 1, và bắt đầu lúc thở 3 giây.

## Ý cốt lõi

### clear-check | AI 8 | Đợt dọn sạch khi đã ra hết và số quái sống của đợt bằng 0
Xét mỗi lần một con quái bị hạ, sau khi mục cuối danh sách đã ra. Ai hạ cũng tính.

### clear-bonus | AI 8 | Thưởng dọn đợt w là 20 + 5×w xu (đợt 1: 25 xu, đợt 10: 70 xu, đợt 20: 120 xu)
Cộng vào `feature-wallet` một lần, ngay lúc dọn sạch. Không phụ thuộc máu thành hay hero còn sống.

### clear-score-plus | AI 8 | Mỗi lần dọn sạch, điểm của lượt tăng đúng 1 (dọn xong đợt 5 thì điểm là 5)
Báo cho `feature-run`; node này không giữ điểm.

### clear-order | AI 7 | Thứ tự trong cùng một lúc: cộng thưởng, báo điểm, rồi bắt đầu lúc thở
Người chơi thấy tiền và điểm cùng lúc, tiêu được tiền thưởng ngay trong lúc thở.

### lose-first | AI 8 | Thành bị phá cùng lúc với con quái cuối bị hạ thì tính là thua, đợt đó không được dọn sạch
Không cộng thưởng, điểm không tăng. Luật đơn giản, không gây tranh cãi về "kịp hay không".
