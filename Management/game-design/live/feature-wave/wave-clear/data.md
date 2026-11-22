---
based-on: 1a74f8c24f86
---
# Dọn sạch đợt

Lúc một đợt kết thúc: quái của đợt đã ra hết và đã bị hạ hết. Người chơi được thưởng tiền, điểm của lượt tăng một đợt, rồi sang lúc thở trước đợt sau.

## Ý cốt lõi

### clear-condition | AI 8 | Đợt được dọn sạch khi quái của đợt đã ra hết và không còn con nào sống
Quái bị hạ bằng hero hay bằng thành đều tính. Đã loại: hết giờ của đợt là xong (quái sót lại làm rối ranh giới đợt).

### clear-reward | AI 8 | Dọn sạch đợt thì được một khoản tiền thưởng, đợt càng sau thưởng càng nhiều
Tiền vào `feature-wallet`. Thưởng không phụ thuộc máu thành còn lại. Đã loại: thưởng thêm khi thành không mất máu (phạt thêm người đang yếu).

### clear-score | AI 8 | Mỗi đợt dọn sạch làm điểm của lượt tăng một
Điểm và kỉ lục giữ ở `feature-run`; node này chỉ báo "đã dọn sạch đợt". Đã loại: điểm theo số quái hạ.

### clear-next | AI 7 | Dọn sạch đợt thì chuyển sang lúc thở, rồi tới đợt kế tiếp
Lúc thở là `feature-wave/wave-gap`. Đã loại: chuyển sang màn nghỉ để nâng cấp (người đã loại ở vòng 1).
