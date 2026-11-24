---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: world-cartoon
design: feature-hero/hero-list/hero-roster-values
---
# Claude concept: năm hero và tầm đánh

## Mô tả
Hình tham chiếu mức IDEA cho năm hero và tầm đánh của từng hero, vẽ theo style `world-cartoon`, nhìn ngang, quay mặt ra xa thành. Bố cục lấy đúng frame `hero-reach`, đọc bằng `python Management/tools/gd_frame.py live/feature-hero/hero-list/hero-roster-values`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in: mỗi hero là một phần riêng, vẽ vừa hộp của nó, hình đọc được vai trò (đúng kiểu đòn ghi trong node). Thành là hình mượn: dùng `same-as` phần thành nếu vừa, không thì tool giữ hình cơ bản. Vòng tầm, mặt đất, nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- hero-reach: năm hero và tầm đánh, đúng vị trí và cỡ của frame `hero-reach`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; mỗi hero lấp hộp của nó, chân chạm mặt đất của hàng mình.
- Năm hero phân biệt được bằng dáng và màu khi thu nhỏ, khác hẳn quái; viền tối rõ; nền phần trong suốt.
