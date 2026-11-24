---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: world-cartoon
design: feature-wave/spawning/spawn-order
---
# Claude concept: điểm ra quái

## Mô tả
Hình tham chiếu mức IDEA cho hai điểm ra quái ngoài mép màn, vẽ theo style `world-cartoon`, nhìn ngang. Bố cục lấy đúng frame `spawn-points`, đọc bằng `python Management/tools/gd_frame.py live/feature-wave/spawning/spawn-order`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Thành và quái là hình mượn: dùng `same-as` phần đã có nếu vừa, không thì tool giữ hình cơ bản. Mặt đất, mũi tên, nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- spawn-points: hai điểm ra quái hai bên, đúng vị trí và cỡ của frame `spawn-points`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; mọi phần vẽ đúng hộp của frame.
- Nhìn ngang, phẳng, không phối cảnh; viền tối rõ; nền phần trong suốt.
