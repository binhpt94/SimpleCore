---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: world-cartoon
design: feature-hero/hero-respawn/respawn-values
---
# Claude concept: chỗ sinh và hồi sinh hero

## Mô tả
Hình tham chiếu mức IDEA cho chỗ sinh và hồi sinh của 3 hero ở mép thành, vẽ theo style `world-cartoon`, nhìn ngang. Bố cục lấy đúng frame `spawn-slots`, đọc bằng `python Management/tools/gd_frame.py live/feature-hero/hero-respawn/respawn-values`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Thành và hộp hero là hình mượn: dùng `same-as` phần đã có nếu vừa, không thì tool giữ hình cơ bản. Mặt đất, nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- spawn-slots: ba chỗ sinh hero ở mép thành, đúng vị trí và cỡ của frame `spawn-slots`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; mọi phần vẽ đúng hộp của frame.
- Nhìn ngang, phẳng, không phối cảnh; viền tối rõ; nền phần trong suốt.
