---
tool: claudeimage
kind: art
count: 1
deliver: concept
format: png 1920x1080
style: world-cartoon
design: feature-cube-test/cube-cases/cube-spec
---
# Claude concept: ba khối thử trên chiến trường

## Mô tả
Hình tham chiếu mức IDEA cho ba khối lập phương thử, vẽ theo style `world-cartoon`, nhìn ngang. Bố cục lấy đúng frame `cubes`, đọc bằng `python Management/tools/gd_frame.py live/feature-cube-test/cube-cases/cube-spec`.

Chỉ vẽ phần node này tự định (nhóm "của node" trong `gd_frame.py`): ba khối `cube-back`, `cube-mid`, `cube-front` cạnh 1u, và ba bóng `shadow-*`. Bóng vẽ thành hình bầu dục phẳng trong hộp 1.2u x 0.25u, độ đục khoảng 30%. Khối màu Vàng của bảng màu chung, viền là màu viền của Vàng; được vẽ khối hơi nghiêng cho thấy mặt trên, nhưng vừa hộp 1u của frame. Mặt đất và thành là hình mượn của node khác, tool giữ hình cơ bản. Các vạch cỏ, đá, đường HUD là nét đo, tool tự vẽ; không vẽ thành phần. Không chép chú thích hay số đo của frame vào ảnh.

## Danh sách
- cubes: ba khối thử và bóng của chúng ở +1.5u (sau thành), −5u và +5u, đúng vị trí và cỡ của frame `cubes`

## Tiêu chí nhận
- Ảnh ghép đúng kích thước tool dựng từ frame; mỗi khối đúng hộp 1u, đáy chạm mặt đất.
- Mặt trên sáng nhất, tối dần xuống dưới; viền rõ khi thu nhỏ.
- Nền phần trong suốt, không vẽ trời hay đất phủ kín phần.
