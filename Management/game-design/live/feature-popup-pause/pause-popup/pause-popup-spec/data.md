---
based-on: 25d160950007
refs: feature-run/run-pause@f8e9d49f5079, feature-run/run-pause/pause-rules@2f184465270c, feature-popup/popup-layer/stack-rules@fa85847e35ee, feature-hud/hud-layout/layout-spec@ebefd7f8b116
---
# Hai bước của popup tạm dừng

Một popup tạm dừng có hai bước trong cùng popup: bước chính (tiêu đề "Tạm dừng", nút "Tiếp tục", nút "Bỏ lượt") và bước hỏi xác nhận (câu hỏi, nút "Bỏ lượt", nút "Không"). Mỗi bước 2 nút.

## Ý cốt lõi

### step-main | AI 7 | Bước chính có tiêu đề "Tạm dừng" và 2 nút: "Tiếp tục" (nút chính) và "Bỏ lượt"
Popup luôn mở ở bước chính.

### resume-tap | AI 7 | Chạm "Tiếp tục" thì báo `feature-run/run-pause` tiếp tục; popup đóng và trận chạy ngay

### quit-to-confirm | AI 6 | Chạm "Bỏ lượt" ở bước chính thì popup đổi sang bước hỏi, không mở popup thứ hai
Bước hỏi có câu "Bỏ lượt này? Điểm tính theo số đợt đã dọn." và 2 nút: "Bỏ lượt" và "Không" (nút chính).

### confirm-yes | AI 7 | Chạm "Bỏ lượt" ở bước hỏi thì báo `feature-run/run-pause` bỏ lượt; popup đóng, về màn bắt đầu

### confirm-no | AI 6 | Chạm "Không" ở bước hỏi thì quay lại bước chính; trận vẫn tạm dừng

### back-key | AI 6 | Nút quay lại của máy: ở bước chính như chạm "Tiếp tục", ở bước hỏi như chạm "Không"

### no-backdrop-close | AI 6 | Chạm nền che không đóng popup và không đổi bước

### one-report-per-choice | AI 7 | Sau khi đã báo tiếp tục hoặc bỏ lượt, mọi nút của popup không nhận chạm nữa
