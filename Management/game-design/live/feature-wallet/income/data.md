---
based-on: 5ba24132c4d3
refs: feature-wallet/balance-spend@df494e209c1e
---
# Nguồn thu

Tiền vào ví từ đúng hai nguồn: hạ quái và dọn sạch đợt. Tiền vào thẳng số dư ngay lúc xảy ra, người chơi không phải nhặt.

## Ý cốt lõi

### income-sources | AI 8 | Có đúng hai nguồn thu: hạ một quái và dọn sạch một đợt
Đã loại: thưởng theo thời gian sống, tiền rơi ngẫu nhiên.

### kill-reward-by-type | AI 8 | Mỗi loại quái cho một mức tiền riêng, quái trâu hơn cho nhiều hơn
Mức tiền là số liệu của từng loại quái trong `feature-enemy`; ví chỉ cộng. Đã loại: mọi quái cùng một mức.

### kill-reward-any-killer | AI 8 | Quái bị hạ bởi hero, đòn thành hay kĩ năng thành đều cho cùng mức tiền
Người chơi không phải lo ai ra đòn cuối. Đã loại: chỉ thành hoặc chỉ hero mới được tiền.

### wave-clear-reward | AI 7 | Dọn sạch một đợt thì được một khoản thưởng, đợt sau thưởng nhiều hơn đợt trước
Mức thưởng do `feature-wave` quyết theo số đợt. Đã loại: thưởng bằng nhau mọi đợt.

### income-instant | AI 8 | Tiền vào số dư ngay khi quái gục hoặc đợt được dọn, không cần nhặt
Đã loại (đã loại ở vòng 2): chạm để nhặt tiền.
