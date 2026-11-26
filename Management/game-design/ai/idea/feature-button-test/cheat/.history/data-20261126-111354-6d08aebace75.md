---
based-on: 84bd60110434
refs: feature-button-test/test-button/button-spec@dcef7b15d85b, feature-hud/hud-layout/layout-spec@1a7ddf5f3b6c
---
# Cheat: nút thử

State dựng nhanh nút thử ở bản dev để test, Play và xem ảnh, không phải chọn đội và vào trận.

## Ý cốt lõi

### pressed-12 | AI 7 | Màn trận có nút "Thử" và nhãn "Đã bấm: 12"
Ép: N = 12 (hai chữ số, thấy được nhãn đã đổi sau nhiều lần bấm). Nút và nhãn đúng chỗ của frame ở `feature-button-test/test-button/button-spec`; HUD trận vẽ ở chỗ của `feature-hud/hud-layout/layout-spec` để thấy nút không che HUD. Không cần trận chạy phía sau. Bấm nút trong state này vẫn tăng N như thường.
