# Thế giới hoạt hình 2D Colorful

## Dùng cho
Thế giới trận: hero, quái, thành, nền chiến trường, hiệu ứng đòn đánh và kĩ năng.
Cùng tông Colorful với giao diện (`ui-flat`) để trận và UI trông là một game.

## Góc nhìn
2D nhìn ngang từ bên cạnh, mặt đất là một đường ngang. Nhân vật và quái vẽ nghiêng ba phần tư về phía hướng đi; hero và quái mặc định quay phải, game tự lật khi đi sang trái. Thành vẽ chính diện, đối xứng trái phải.

## Bảng màu
- Lấy màu từ bảng màu chung ở `ui-flat` (mã màu thân, vùng sáng, bóng, viền). Màu riêng của trận (phe quái, nền, kĩ năng) thêm vào bảng đó, không tự chọn khi vẽ.
- Hero dùng tông ấm và sáng (vàng, cam, đỏ, xanh dương sáng); quái dùng tím, xanh lá đậm, xanh lam tối, để phân biệt phe chỉ bằng màu.
- Nền chiến trường tươi nhưng nhạt và ít bão hoà hơn nhân vật một bậc (trời xanh sáng, cỏ xanh lá), để hero và quái nổi lên trên nền.

## Nét viền
- Viền ngoài đậm, đều, dày tối thiểu 4 px khi sprite cao 256 px; màu viền là bản đậm của màu thân sát viền (cột "Viền" của bảng màu chung), không đen tuyền.
- Viền thành, hero và quái đạt tương phản tối thiểu 3:1 với lớp nền ngay sau chúng, kiểm ở cả hai cánh (trước trời và trước cảnh xa). Không đạt thì chỉnh màu nền trước, rồi mới tới viền sprite.
- Chi tiết bên trong viền mảnh hơn viền ngoài.

## Độ chi tiết
- Chibi: tỉ lệ đầu to, thân ngắn (khoảng 1:2 đầu so với thân), mắt to, mặt biểu cảm rõ; dáng đọc được khi chỉ nhìn bóng đen.
- Mỗi hero và mỗi loại quái có một nét nhận dạng lớn (vũ khí, mũ, hình khối) thấy rõ khi sprite cao 128 px.
- Hero và quái phân biệt được phe chỉ bằng hình bóng tô đen ở kích thước trong trận (ví dụ hero dáng đứng thẳng, mũ giáp; quái dáng khom, tai sừng). Vũ khí không đủ: cung thủ có ở cả hai phe.
- Thân thành ở tầm cao của hero dùng mảng màu liền ở hai mép giáp dải đất, không hoa văn rối, để nhân vật đứng sát thành vẫn rõ hình.
- Mỗi nhân vật là một hình đứng yên; chuyển động do game tạo bằng scale, move, rotate hoặc đổi màu, không cần khung hình.

## Ánh sáng
Một nguồn sáng từ trên, giống giao diện: mép trên của mỗi khối sáng nhất, tối dần về mép dưới. Khối tròn trịa, chuyển sáng tối mềm trong cùng sắc màu, có một vệt bóng sáng nhỏ (như kẹo) ở chỗ cao nhất. Bóng đổ dưới chân là một hình bầu dục tối, không vẽ vào sprite.

## Làm
- Sprite nền trong suốt, mỗi file một nhân vật hoặc một vật.
- Thành, hero và quái vẽ cùng một tỉ lệ để đặt cạnh nhau không lệch cỡ.
- Hiệu ứng (sấm, băng, nổ) là hình khối rõ, màu tươi, có viền, kèm sao hoặc lấp lánh; đọc được trong một khoảnh khắc.
- Hiệu ứng vẽ đè lên thành, hero hay quái: độ đục cộng gộp tối đa 50% sau khi ghép, kể cả khi nhiều hiệu ứng trùng nhau. Điểm sáng mạnh đặt ở vùng trống quanh mục tiêu.
- Hiệu ứng kĩ năng thành dùng đúng màu chủ đạo của kĩ năng đó trên nút HUD.
- Đối chiếu mọi asset với một khung trận ngang mẫu đã duyệt: thành, hero, quái, nền, hiệu ứng, HUD và thanh nâng cấp ở kích thước hiển thị thật, theo bố cục đã chốt. Khung mẫu dựng từ concept nền chiến trường và HUD sau khi người duyệt.

## Không làm
- Không vẽ kiểu thật, không phối cảnh 3D, không nhìn từ trên xuống.
- Không màu đục, xám, tối toàn thân (quái tối hơn hero nhưng vẫn tươi).
- Không texture chi tiết, không viền mờ hay viền nhạt hơn thân.
- Không vẽ nền phía sau nhân vật trong cùng một file.
