---
tool: codex
kind: image
count: 1
deliver: Private
format: png 200x480, nền trong suốt
references: Resources/_design/feature-popup-lose.cheat/codex/lose-popup-colorful/v2/new-record.png
design: feature-battlefield/battlefield-layers/layer-values
---
# Sprite decor đứng: cờ của nền chiến trường theo style Colorful (PROTOTYPE)
## Mô tả
Decor đứng trong dải cảnh xa, style Colorful `world-cartoon`; dùng cho 4 cây cờ (mỗi bên thành 2). Đứng trên mặt đất nhưng vẽ sau dải đất và trước dải cảnh xa, nên đọc là cảnh ở xa, không phải vật cản.
- Trong game cỡ 0.5×1.2u (khoảng 30x72 px trên 1080p), chân trên mặt đất.
- Màu: lá cờ Hồng #F24FA0, cột Vàng #FFC61F của bảng màu chung, **cả hình đã pha 20% về màu trời #7CD0FF** (vẽ sẵn trong ảnh), cùng độ nhạt với dải cảnh xa trước.
- Style như ảnh tham chiếu: khối tròn mềm, sáng ở mép trên, viền là bản đậm của màu thân, không đen tuyền; đầu cột có núm tròn vàng.
- Lá cờ bay ngang về một phía, đứng yên (không cần khung hình). Không bóng đổ, không chữ, không số, không kí hiệu hay huy hiệu trên cờ.
## Danh sách
- flag: một cây cờ nhỏ: cột vàng thẳng đứng có núm tròn trên đỉnh, lá cờ hồng hình tam giác đuôi én gắn ở phần trên cột; chân cột chạm mép dưới, cột ở giữa ngang
## Tiêu chí nhận
- Đúng 200x480; nền trong suốt với alpha 0 ngoài hình, bốn góc trong suốt.
- Chân cột chạm mép dưới (cách không quá 4 px); đỉnh núm cách mép trên không quá 16 px.
- Nhận ra là cây cờ khi thu xuống 30x72 px; không chữ, số, kí tự hay huy hiệu.
