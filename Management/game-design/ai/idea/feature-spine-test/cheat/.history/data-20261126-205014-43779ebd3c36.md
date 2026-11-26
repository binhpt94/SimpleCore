---
based-on: 5033d18c216a
refs: feature-spine-test/spine-checks/spine-spec@396ed200581f, feature-hud/hud-layout/layout-spec@1a7ddf5f3b6c
---
# Cheat: nhân vật thử Spine

State dựng nhanh nhân vật Spine và các nút thử ở bản dev để test, Play và xem ảnh, không phải chọn đội và vào trận.

## Ý cốt lõi

### run-flipped | AI 7 | Màn trận có nhân vật Spine đang chạy, quay mặt phải, nhãn "run · lặp · mặt phải"
Ép: animation run, mặt phải, nút "Chạy" ở trạng thái đang chọn. Nhân vật, bốn nút và nhãn đúng chỗ của `feature-spine-test/spine-checks/spine-spec`. Không cần trận chạy phía sau. Bấm nút trong state này vẫn đổi animation và lật mặt như thường.
