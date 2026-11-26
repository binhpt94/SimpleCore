---
based-on: 5e6dc1e8532e
refs: feature-wave/wave-gap@d9d4c35f6907, feature-hud/hud-layout/layout-spec@aa79f655e44b
---
# Ba ô thông tin trận và thông báo đợt

HUD trận có 3 ô thông tin: máu thành (thanh kèm số), tiền (biểu tượng xu kèm số), đợt ("Đợt N"). Đầu mỗi đợt hiện thông báo "Đợt N" một lúc ngắn. Mọi số đọc từ feature chủ.

## Ý cốt lõi

### hp-bar-and-number | AI 8 | Máu thành hiện bằng một thanh và chữ "hiện tại/tối đa", ví dụ "750/1000"
Thanh dài theo tỉ lệ máu hiện tại trên máu tối đa. Số là số nguyên, làm tròn xuống; thành bị phá hiện "0/…".

### hp-max-change | AI 7 | Nâng máu tối đa thì số tối đa và tỉ lệ thanh đổi ngay trong khung hình nhận báo
Ví dụ 750/1000 nâng thành 850/1100: số đổi, thanh tính lại theo tỉ lệ mới.

### money-icon-number | AI 8 | Tiền hiện bằng 1 biểu tượng xu và số nguyên đầy đủ, ví dụ "50"
Biểu tượng xu đại diện cho tiền trong lượt (`feature-wallet`). Đổi ngay khi nhận hay tiêu.

### wave-label | AI 7 | Ô đợt hiện chữ "Đợt N", N là đợt đang đánh; trong 3 giây nghỉ thì N là đợt sắp tới
Đầu lượt hiện "Đợt 1".

### wave-banner | AI 6 | Mỗi lần một đợt bắt đầu, thông báo "Đợt N" hiện 1.5 giây rồi tự tắt, không chặn bấm
Không che thao tác: chạm xuyên qua thông báo vẫn tới nút bên dưới.

### status-no-input | AI 8 | Ba ô thông tin và thông báo đợt không nhận chạm, không tự tính số nào
Máu từ `feature-castle`, tiền từ `feature-wallet`, đợt từ `feature-wave`.
