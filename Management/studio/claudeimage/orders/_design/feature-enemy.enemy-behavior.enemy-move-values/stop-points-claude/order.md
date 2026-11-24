---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: world-cartoon
design: feature-enemy/enemy-behavior/enemy-move-values
---
# Claude concept: chỗ quái dừng đánh thành

## Mô tả
Hình tham chiếu mức IDEA cho chỗ từng loại quái dừng lại đánh thành (bên phải; bên trái đối xứng), vẽ theo style `world-cartoon`, nhìn ngang. Bố cục lấy đúng frame `stop-points`, đọc bằng `python Management/tools/gd_frame.py live/feature-enemy/enemy-behavior/enemy-move-values`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Hộp quái và thành là hình mượn của node khác: dùng `same-as` phần đã có nếu vừa, không thì tool giữ hình cơ bản. Mặt đất, nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- stop-points: chỗ dừng đánh thành của từng loại quái, đúng vị trí và cỡ của frame `stop-points`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; mọi phần vẽ đúng hộp của frame, chân chạm mặt đất của hàng mình.
- Nhìn ngang, phẳng, không phối cảnh; viền tối rõ; nền phần trong suốt.
