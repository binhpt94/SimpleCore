---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: world-cartoon
design: feature-castle/castle-attack/auto-shot-values
---
# Claude concept: tầm tự bắn của thành

## Mô tả
Hình tham chiếu mức IDEA cho tầm tự bắn của thành trên chiến trường, vẽ theo style `world-cartoon`, nhìn ngang. Bố cục lấy đúng frame `shot-range`, đọc bằng `python Management/tools/gd_frame.py live/feature-castle/castle-attack/auto-shot-values`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Thân thành là hình mượn của `castle-body-values`: dùng `same-as` phần thành đã có nếu vừa, không thì tool giữ hình cơ bản. Vòng tầm, mặt đất, nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- shot-range: tầm tự bắn hai bên thành, đúng vị trí và cỡ của frame `shot-range`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; mọi phần vẽ đúng hộp của frame, chân chạm mặt đất.
- Nhìn ngang, phẳng, không phối cảnh; viền tối rõ; nền phần trong suốt.
