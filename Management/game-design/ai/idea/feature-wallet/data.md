---
based-on: 6181a7c21bf0
by: wallet-balance=gpt@r2-1#wallet-balance, run-scoped=gpt@r2-1#wallet-run-scope
---
# Tiền

Loại tiền duy nhất của lượt chơi: nhận khi hạ quái và khi dọn sạch đợt, tiêu để nâng cấp ngay trong trận. Tiền chỉ sống trong một lượt chơi.

## Ý cốt lõi

### single-currency | AI 8 | Game chỉ có một loại tiền
Đã loại: đá quý hay tiền lâu dài (game không có tiến trình lâu dài).

### wallet-balance | AI 6 | Ví giữ số dư tiền người chơi đang có, dùng để nhận thưởng và mua nâng cấp
Quái và đợt quyết thưởng bao nhiêu, nâng cấp quyết món và giá; ví chỉ giữ số tiền.

### earn | AI 8 | Nhận tiền khi hạ quái và khi dọn sạch một đợt

### wallet-records-rewards | AI 7 | Ví là nơi duy nhất ghi khoản thưởng tiền khi quái bị hạ hoặc đợt được dọn sạch
`feature-enemy` xác nhận quái bị hạ, `feature-wave` xác nhận đợt đã dọn; ví ghi khoản thưởng vào số dư. Theo câu hỏi chung vòng 2 (`chu-so-huu-thuong-tien`).

### spend | AI 8 | Tiêu tiền để mua nâng cấp; không đủ tiền thì không mua được

### run-scoped | AI 7 | Mỗi lượt có số dư riêng, dùng chung qua các đợt trong lượt; lượt mới bắt đầu lại
Để dành tiền từ đợt trước để nâng cấp khi bị dồn ép ở đợt sau được.
