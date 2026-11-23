---
based-on: e712e2fbe1f0
refs: feature-wallet/balance-spend@df494e209c1e, feature-upgrade/upgrade-effect@b9bcec737959
---
# Mua nâng cấp

Luật một lần mua: mua được lúc nào, giá bao nhiêu, tiền trừ ra sao. Mỗi lần mua là một cấp của một mục, trả bằng tiền trong ví.

## Ý cốt lõi

### buy-anytime | AI 8 | Mua được mọi lúc trong trận, kể cả giữa đợt, trừ lúc đang tạm dừng
Đã loại (người loại ở vòng 1): chỉ mua giữa hai đợt. Đã loại: mua cả lúc tạm dừng (mất áp lực chọn giữa trận).

### buy-one-level | AI 8 | Mỗi lần mua tăng đúng một cấp của một mục
Đã loại: mua nhiều cấp một lúc.

### buy-pay-wallet | AI 8 | Mua thì xin ví trừ giá cấp sau; ví trừ được mới lên cấp, không đủ thì không có gì đổi
Ví giữ luật không âm (`feature-wallet/balance-spend`).

### cost-rises | AI 7 | Giá cấp sau của một mục cao hơn giá cấp trước của chính mục đó
Mỗi mục tăng giá riêng, mua mục này không làm mục khác đắt lên. Đã loại: giá cố định; mua bất kì mục nào cũng làm mọi mục đắt lên.

### no-cap-no-refund | AI 7 | Không có cấp tối đa và không bán lại cấp đã mua
Giá tăng dần tự hãm. Đã loại: cấp tối đa; bán lại hoàn tiền.
