---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: world-cartoon
design: feature-castle/castle-core/castle-body-values
---
# Claude concept: thân thành

## Mô tả
Hình tham chiếu mức IDEA cho thân thành trên chiến trường, vẽ theo style `world-cartoon`, nhìn ngang. Bố cục lấy đúng frame `castle`, đọc bằng `python Management/tools/gd_frame.py live/feature-castle/castle-core/castle-body-values`.

Chỉ vẽ phần node này tự định (nhóm "của node" trong `gd_frame.py`): thân thành rộng 3u, cao 4u, đặt trên mặt đất. Mặt đất, mép thành và mép màn là nét đo, tool tự vẽ. Chỗ đứng của hero là hình mượn của node khác, tool giữ hình cơ bản. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- castle: thân thành ở giữa chiến trường, đúng vị trí và cỡ của frame `castle`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; thân thành đúng hộp 3u x 4u của frame, chân chạm mặt đất.
- Nhìn ngang, phẳng, không phối cảnh; viền tối rõ, đọc được hình thành khi thu nhỏ.
- Nền phần trong suốt, không vẽ trời hay đất phủ kín phần.
