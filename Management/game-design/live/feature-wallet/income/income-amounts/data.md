---
based-on: d2e78e0c80dc
refs: feature-wallet/balance-spend/wallet-rules@1367fdb78322, feature-enemy/enemy-reward/reward-values@30a4a6c88e50, feature-wave/wave-clear/clear-rules@b10f4f27eaf5, feature-run/run-start/start-values@ecc0af822edd
frame: none
---
# Mức tiền thu

Ví nhận tiền thu thế nào: cộng đúng số bên báo đưa, ngay khi được gọi, theo thứ tự tới. Mức tiền hạ quái và thưởng dọn đợt do nhánh quái và nhánh đợt quyết, ví không giữ bảng.

## Ý cốt lõi

### amounts-from-caller | AI 8 | Ví không giữ bảng tiền hạ quái hay công thức thưởng dọn đợt; mức tiền do bên báo đưa vào
Tiền hạ từng loại quái ở `feature-enemy/enemy-reward/reward-values`, thưởng dọn đợt ở `feature-wave/wave-clear/clear-rules`. Đã loại: ví tra bảng theo loại quái hay tự tính thưởng theo số đợt.

### add-once-per-call | AI 8 | Mỗi lần bên báo gọi cộng tiền là đúng một lần cộng, đúng bằng số được đưa
Ví không gộp, không bỏ, không nhân. Việc mỗi quái hay mỗi đợt chỉ báo một lần là của bên báo.

### income-same-frame | AI 7 | Tiền vào số dư trong cùng khung hình ví được gọi cộng
Không có độ trễ, không có tiền rơi phải nhặt. Số dư mới được báo ngay theo `feature-wallet/balance-spend/wallet-rules`.

### add-in-call-order | AI 7 | Nhiều lần cộng trong cùng khung hình được cộng lần lượt theo thứ tự gọi tới
Ví dụ con quái cuối của đợt gục: tiền con quái tới trước, thưởng dọn đợt tới sau, theo thứ tự của `feature-wave/wave-clear/clear-rules`.

### ignore-after-run-end | AI 6 | Lời gọi cộng tới sau khi lượt đã kết thúc thì ví bỏ qua, số dư giữ nguyên
Quái gục cùng khung hình thành bị phá không được cộng, theo `feature-enemy/enemy-reward/reward-values` (thành bị phá xử lí trước). Tiền của lượt cũ tới muộn cũng không vào lượt mới (`feature-run/run-start/start-values`).
