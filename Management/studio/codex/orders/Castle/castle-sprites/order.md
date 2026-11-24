---
tool: codex
kind: image
count: 3
deliver: Private
format: png 512x512, nền trong suốt
design: feature-castle/castle-core/castle-body-values
---
# Sprite thành, đạn tự bắn và hiệu ứng Sấm sét (mức IDEA)

## Mô tả
Sprite cho feature Castle trong game thủ thành 2D nhìn ngang, màn ngang 1920x1080. Style `world-cartoon` của game:
- hoạt hình 2D nhìn ngang, viền ngoài tối và đều, dày tối thiểu 4 px khi sprite cao 256 px;
- màu tươi, bão hoà vừa phải, đổ bóng 2 bậc (sáng và tối) với nguồn sáng từ trên bên trái;
- không gradient mịn, không texture chi tiết, không phối cảnh 3D, không vẽ nền phía sau.

Mỗi file là một vật, nền trong suốt. Trong game, mọi chuyển động (nháy, thu nhỏ, bay) do code làm bằng scale, move và đổi màu.

Kích thước thật trong game:
- Thành rộng 3u, cao 4u, đứng giữa màn; màn thấy 32u bề ngang, nên thành cao khoảng 270 px trên 1080p.
- Đạn khoảng 0.3u.
- Hiệu ứng Sấm sét khoảng 1.5u, hiện trên đầu quái bị trúng.

## Danh sách
- castle: tòa thành đá kiểu hoạt hình, vẽ chính diện, đối xứng trái phải; tỉ lệ rộng:cao đúng 3:4; chân thành chạm sát mép dưới ảnh, phủ kín bề ngang theo tỉ lệ 3:4 và căn giữa; có cổng ở giữa, hai tháp hai bên, tường đá xám ấm, mái hoặc cờ màu ấm
- shot: viên đạn năng lượng tròn nhỏ màu vàng cam, có viền tối, căn giữa ảnh, chiếm khoảng 60% ảnh
- thunder: tia sét đánh xuống, khối rõ, màu xanh trắng, có viền tối, kèm chớp nổ nhỏ ở chân tia; căn giữa ảnh, chiếm khoảng 80% chiều cao

## Tiêu chí nhận
- Đúng 512x512, nền trong suốt (alpha 0 ngoài vật), không có nền, không có bóng đổ dưới chân.
- castle: phần không trong suốt có tỉ lệ rộng:cao 3:4 (lệch không quá 5%); chân thành chạm mép dưới ảnh (cách không quá 8 px); đối xứng trái phải; nhìn chính diện, không phối cảnh.
- Viền ngoài tối, liền, dày tối thiểu 4 px ở 512 px; không viền mờ hay viền nhạt hơn thân.
- shot và thunder đọc rõ khi thu xuống 32 px; thunder nhận ra là tia sét khi thu xuống 96 px.
- Không chữ, số hay kí tự nào trong ảnh.
