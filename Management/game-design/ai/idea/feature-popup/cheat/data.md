---
based-on: 04fa62031f0e
refs: feature-popup/popup-layer/stack-rules@fa85847e35ee, feature-popup/popup-request/request-rules@9e6b3fbcf68a
---
# Cheat: dịch vụ popup

State dựng nhanh lớp popup ở bản dev để test, Play và gen ảnh concept: một popup đang mở chồng trên trận, thấy nền che và khung popup chuẩn.

## Ý cốt lõi

### popup-over-battle | AI 7 | Một popup đang mở trên trận: màn chơi và HUD bên dưới, nền che, khung popup trên cùng
Ép: mở popup `PopupPause/Main` giữa trận (trận đứng yên). Thấy đủ ba lớp của dịch vụ: màn chơi và HUD không nhận bấm, nền che tối, khung popup chuẩn nhận chạm. Nội dung trong khung là của feature gọi, không vẽ ở đây.
