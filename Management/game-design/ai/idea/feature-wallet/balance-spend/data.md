---
based-on: 5ba24132c4d3
refs: feature-wallet/income@d2e78e0c80dc, feature-upgrade/upgrade-purchase@c53d76edf8b3
---
# Số dư và chi tiêu

Số dư là lượng tiền người chơi đang có trong lượt. Feature này giữ số dư, cộng khi có nguồn thu, trừ khi mua và chỉ trừ khi đủ.

## Ý cốt lõi

### one-balance | AI 8 | Người chơi có đúng một số dư, là một số nguyên không âm
Một loại tiền nên chỉ cần một con số. Đã loại: nhiều ví theo nguồn thu.

### spend-if-enough | AI 8 | Chỉ trừ tiền khi số dư lớn hơn hoặc bằng giá; không đủ thì giao dịch bị từ chối và số dư giữ nguyên
Nhờ vậy số dư không bao giờ âm. Đã loại: cho nợ, mua trước trả sau.

### spend-one-asker | AI 7 | Chỉ feature-upgrade tiêu tiền; ví không biết món mua là gì, chỉ biết giá
Ví chỉ trả lời "trừ được hay không". Giá do `feature-upgrade` đưa vào. Đã loại: ví giữ bảng giá.

### run-start-amount | AI 7 | Đầu mỗi lượt số dư về số tiền ban đầu, do feature-run ra lệnh đặt lại
Số tiền ban đầu là số liệu cấu hình của ví (`feature-balance` quyết). Tiền không giữ qua lượt sau. Đã loại: ví tự đặt lại khi thấy trận mới.

### balance-changed | AI 7 | Mỗi lần số dư đổi, ví báo số mới cho ai đang theo dõi
Người theo dõi là `feature-hud` (hiện số tiền) và `feature-hud-upgrade` (làm mờ nút không đủ tiền). Đã loại: các bên tự hỏi số dư liên tục.
