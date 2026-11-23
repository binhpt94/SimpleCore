---
based-on: 700960590953
refs: feature-wave/difficulty-scaling@fed0ac86d57c, feature-wave/wave-clear@b7789c127bb4, feature-balance/balance-difficulty-curve@f1f04ceb7020
---
# Thu và chi

Quan hệ giữa tiền người chơi nhận được (hạ quái, dọn sạch đợt) và giá nâng cấp tăng theo cấp. Tiền tới đều để luôn có thứ đáng mua, nhưng không bao giờ đủ mua hết.

## Ý cốt lõi

### econ-kill-main | AI 7 | Tiền hạ quái là nguồn chính, thưởng dọn sạch đợt là phần thêm nhỏ hơn
Tiền tới đều trong lúc đánh, hợp với nâng cấp mua mọi lúc. Đã loại: thưởng dọn đợt là nguồn chính (mua dồn cuối đợt, giống nâng cấp giữa hai đợt mà người đã loại).

### econ-steady-buy | AI 7 | Trong mỗi đợt người chơi thường đủ tiền mua ít nhất một nâng cấp
Luôn có quyết định để làm trong trận. Đã loại: phải dành nhiều đợt mới mua được một lần.

### econ-never-all | AI 8 | Giá tăng theo cấp nhanh hơn thu tăng theo đợt, nên không bao giờ mua hết, phải chọn
Chọn hero nào, thành mục nào là quyết định chính trong trận. Đã loại: thu đủ để nâng đều mọi mục.

### econ-no-best | AI 6 | Không có mục nâng cấp nào luôn lời nhất; giá rẻ ở cấp thấp kéo người chơi rải đều
Đã loại: một mục luôn đáng mua nhất (người chơi chỉ bấm một nút).
