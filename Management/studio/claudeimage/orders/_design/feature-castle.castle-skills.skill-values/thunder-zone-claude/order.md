---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: world-cartoon
design: feature-castle/castle-skills/skill-values
---
# Claude concept: vùng Sấm sét

## Mô tả
Hình tham chiếu mức IDEA cho vùng kĩ năng Sấm sét quanh thành, vẽ theo style `world-cartoon`, nhìn ngang. Bố cục lấy đúng frame `thunder-zone`, đọc bằng `python Management/tools/gd_frame.py live/feature-castle/castle-skills/skill-values`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Thân thành mượn của `castle-body-values`: dùng `same-as` phần thành nếu vừa, không thì tool giữ hình cơ bản. Vòng, mặt đất, nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- thunder-zone: vùng Sấm sét hai bên thành, đúng vị trí và cỡ của frame `thunder-zone`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; mọi phần vẽ đúng hộp của frame.
- Nhìn ngang, phẳng, không phối cảnh; viền tối rõ; nền phần trong suốt.
