---
based-on: d0ede8765995
refs: feature-popup/popup-layer/stack-rules@5f909869fc3d, feature-popup/popup-request/request-rules@2a7ea725bb37, feature-popup-lose/cheat@683571cd6600, feature-popup/popup-request@ff4b78bc4b2c, feature-hud/hud-layout/layout-spec@aa79f655e44b
---
# Cheat: dịch vụ popup

State dựng nhanh lớp popup ở bản dev để test và Play dịch vụ một mình: dịch vụ mở một popup mẫu của chính nó lên trên scene đang chạy, thấy nền che, khung và việc chặn bấm. Cảnh popup đè lên trận nằm ở cheat của feature sở hữu popup (`feature-popup-lose/cheat`).

## Ý cốt lõi

### sample-popup | AI 7 | Popup mẫu của dịch vụ đang mở trên scene đang chạy: phần bên dưới không nhận bấm, nền che, khung popup trống trên cùng
Ép: mở id `_Services/Popup/DevSample` do chính dịch vụ sở hữu, chỉ có ở bản dev; khung trống cỡ khung chuẩn của `stack-rules`, một nút "Đóng" để thử đóng. Không đụng trận: không dừng trận, không cần biết đang ở màn nào. Không mở id của feature khác.
