---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: world-cartoon
design: feature-battlefield/battlefield-layers/layer-values
---
# Claude concept: nền chiến trường

## Mô tả
Hình tham chiếu mức IDEA cho nền chiến trường, vẽ theo style `world-cartoon`, nhìn ngang. Bố cục lấy đúng frame `layers`, đọc bằng `python Management/tools/gd_frame.py live/feature-battlefield/battlefield-layers/layer-values`. Khung frame phủ x −21…+21u để thấy cả phần dành cho màn 21:9.

Chỉ vẽ phần node này tự định (nhóm "của node" trong `gd_frame.py`):
- Trời: phủ từ mặt đất lên mép trên khung.
- Cảnh xa: đồi, núi, rừng thấp, một dải liền trên mặt đất, cao 2.5u.
- Dải đất: từ mặt đất xuống mép dưới khung; mặt trên là đường mặt đất.
- Decor: cỏ và đá phẳng, nằm sát mặt đất, thấp 0.3u. Cỏ dùng chung một phần, đá dùng chung một phần.
Thành là hình mượn của node khác, tool giữ hình cơ bản. Mép màn và hai đường HUD là nét đo, tool tự vẽ. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- layers: nền chiến trường đủ bốn lớp trời, cảnh xa, dải đất, decor, đúng vị trí và cỡ của frame `layers`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; mỗi lớp đúng hộp của frame.
- Cảnh xa thấp hơn hộp thành rõ rệt, không lấn sự chú ý khỏi giữa màn.
- Decor phẳng, thấp, không có cờ, hàng rào hay vật đứng nào trông như vật cản.
- Dải đất và trời lặp hay kéo ngang được (ghép liền khi phủ rộng), không có vật nổi bật ở hai đầu.
- Nhìn ngang, phẳng, không phối cảnh; viền tối theo style.
