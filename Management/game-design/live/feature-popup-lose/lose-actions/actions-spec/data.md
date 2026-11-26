---
based-on: 133ab527ee01
refs: feature-popup-lose/lose-result/result-spec@92e28a5acae5, feature-run/run-flow/flow-rules@593e3d21d6c1, feature-run/run-score/score-rules@2ff455851b82, feature-popup/popup-layer@91d4f017d2fb, feature-popup/popup-layer/stack-rules@5f909869fc3d, feature-hud/hud-layout/layout-spec@aa79f655e44b
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
