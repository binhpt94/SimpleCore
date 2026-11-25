---
based-on: bddc499985f8
refs: feature-popup-lose/lose-result/result-spec@a7d56f47797b, feature-run/run-flow/flow-rules@c2c5ba06ffa8, feature-run/run-score/score-rules@54a8417a7ccd, feature-popup/popup-layer@91d4f017d2fb, feature-popup/popup-layer/stack-rules@fa85847e35ee, feature-hud/hud-layout/layout-spec@ebefd7f8b116
---
# Hai nút của popup thua

Popup thua có đúng 2 nút "Chơi lại" và "Về màn bắt đầu". Chỉ lần chạm đầu có tác dụng; popup không đóng bằng cách nào khác.

## Ý cốt lõi

### two-buttons-text | AI 8 | Có 2 nút: "Chơi lại" là nút chính, "Về màn bắt đầu" là nút phụ
Nút chính đặt trước và nổi hơn nút phụ; cách làm nổi do chuyên gia giao diện quyết.

### replay-report | AI 8 | Chạm "Chơi lại" thì báo `feature-run` chọn chơi lại; `feature-run` đóng popup và sang màn chọn đội

### home-report | AI 8 | Chạm "Về màn bắt đầu" thì báo `feature-run` chọn về màn bắt đầu; `feature-run` đóng popup và sang màn bắt đầu

### first-tap-only | AI 7 | Chỉ lần chạm nút đầu tiên được báo; sau đó cả 2 nút không nhận chạm
Tránh chạm cả hai nút hay chạm một nút hai lần.

### no-other-close | AI 7 | Chạm nền che, chạm ngoài popup hay nút quay lại của máy đều không đóng popup
