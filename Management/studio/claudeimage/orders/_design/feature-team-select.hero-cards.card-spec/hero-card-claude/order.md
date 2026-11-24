---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: ui-flat
design: feature-team-select/hero-cards/card-spec
---
# Claude concept: một thẻ hero

## Mô tả
Hình tham chiếu mức IDEA cho một thẻ hero (thẻ đầu tiên, Hiệp sĩ), vẽ theo style `ui-flat`. Bố cục lấy đúng frame `card`, đọc bằng `python Management/tools/gd_frame.py live/feature-team-select/hero-cards/card-spec`.

Chỉ vẽ các hình thuộc nhóm "của node" mà `gd_frame.py` in. Khung thẻ là phần dùng chung cho cả 5 thẻ (9-slice nếu hợp). Hình hero trên thẻ nếu frame có thì vẽ hình Hiệp sĩ phẳng, rõ nghĩa. Chữ và nét đo tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- card: một thẻ hero Hiệp sĩ, đúng vị trí và cỡ của frame `card`

## Tiêu chí nhận
- Ảnh ghép đúng 1920x1080; mọi phần vẽ đúng vị trí và cỡ của frame, lệch không quá 20 px mỗi cạnh.
- Khung thẻ viền dày tối thiểu 3 px, bóng đổ cứng lệch xuống 4 đến 8 px; không gradient, không glow.
- Hình hero đọc được là hiệp sĩ cầm khiên ở cỡ thật.
