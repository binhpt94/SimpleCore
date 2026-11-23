# Giao diện phẳng

## Dùng cho
UI: HUD (thanh máu thành, ô tiền, ô đợt), nút kĩ năng, thanh nâng cấp, popup, màn bắt đầu, màn chọn đội, icon.

## Góc nhìn
Nhìn thẳng, phẳng, không phối cảnh.

## Bảng màu
- Tối đa 6 màu chính cho toàn bộ giao diện, cộng trắng và đen.
- Nền bảng và popup là một màu đặc, đậm hơn nền trận để nổi trên chiến trường.
- Nút chính và nút phụ khác nhau rõ về độ sáng, không chỉ khác sắc màu.
- Trạng thái "không bấm được" là bản mờ của chính nút đó (giảm độ sáng và độ bão hoà), không đổi sang màu khác.

## Nét viền
- Viền tối thiểu 3 px ở 1080p cho nút, icon và khung popup.
- Góc bo tròn đều; mọi nút cùng một bán kính bo.

## Độ chi tiết
- Icon đọc rõ ở 64x64 px: một hình chính, tối đa 2 chi tiết phụ.
- Chữ trên nút cao tối thiểu 36 px ở 1080p.

## Ánh sáng
Không có nguồn sáng. Được một lớp bóng đổ cứng dưới nút và popup, lệch xuống dưới 4 đến 8 px ở 1080p.

## Làm
- Màu đặc, khối rõ, chữ đậm, tương phản cao với nền trận.
- Cùng một icon cho cùng một thứ ở mọi nơi (ví dụ icon tiền ở HUD và ở nút nâng cấp).

## Không làm
- Không gradient, không texture, không hiệu ứng kính mờ.
- Không bóng đổ mềm, không glow.
- Không chi tiết mảnh hơn 3 px ở 1080p.
- Không vẽ chữ vào ảnh; chữ do game hiển thị.
