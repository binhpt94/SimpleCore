---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: world-cartoon
design: feature-enemy/enemy-list/enemy-type-values
---
# Claude concept: năm loại quái

## Mô tả
Hình tham chiếu mức IDEA cho năm loại quái đứng cạnh nhau so cỡ với hero, vẽ theo style `world-cartoon`, nhìn ngang, quay mặt về phía thành. Bố cục lấy đúng frame `enemy-boxes`, đọc bằng `python Management/tools/gd_frame.py live/feature-enemy/enemy-list/enemy-type-values`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in: mỗi loại quái là một phần riêng, vẽ vừa hộp của nó, hình đọc được loại quái (đúng kiểu đánh ghi trong node). Hero so cỡ là hình mượn, tool giữ hình cơ bản. Mặt đất tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- enemy-boxes: năm loại quái đứng cạnh nhau, đúng vị trí và cỡ của frame `enemy-boxes`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; mỗi quái lấp hộp của nó, chân chạm mặt đất.
- Năm quái phân biệt được bằng dáng và màu khi thu nhỏ; viền tối rõ; nền phần trong suốt.
