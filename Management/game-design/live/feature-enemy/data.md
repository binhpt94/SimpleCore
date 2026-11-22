---
based-on: c266fb040742
---
# Quái

Kẻ địch đi từ mép trái và mép phải màn hình vào phía thành. Quái đánh hero chắn đường và đánh thành nếu tới được; bị hạ thì cho người chơi tiền.

## Ý cốt lõi

### enemy-path | AI 8 | Quái đi từ hai mép màn hình vào thành, đánh hero chắn đường và đánh thành khi tới nơi
Cho hero cận chiến có vai trò chắn. Đã loại: quái bỏ qua hero, chỉ nhắm thành.

### enemy-types | AI 6 | Có nhiều loại quái với cách đi và sức chịu khác nhau
Để đợt sau khác đợt trước và mỗi kiểu hero có chỗ mạnh. Đã loại: một loại quái chỉ tăng chỉ số.

### enemy-reward | AI 8 | Hạ quái thì được tiền, dù hạ bằng hero hay bằng thành
Tiền vào thẳng `feature-wallet`, không cần nhặt.
