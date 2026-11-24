---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: world-cartoon
design: feature-hero/hero-targeting/targeting-values
---
# Claude concept: vùng hero nhắm và đi

## Mô tả
Hình tham chiếu mức IDEA cho vùng hero nhắm quái và vùng hero đi được, vẽ theo style `world-cartoon`, nhìn ngang. Bố cục lấy đúng frame `hero-zones`, đọc bằng `python Management/tools/gd_frame.py live/feature-hero/hero-targeting/targeting-values`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Thành là hình mượn: dùng `same-as` phần thành nếu vừa, không thì tool giữ hình cơ bản. Mặt đất, vùng, nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- hero-zones: vùng nhắm và vùng đi của hero, đúng vị trí và cỡ của frame `hero-zones`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; mọi phần vẽ đúng hộp của frame.
- Hai vùng phân biệt rõ; nhìn ngang, phẳng; nền phần trong suốt.
