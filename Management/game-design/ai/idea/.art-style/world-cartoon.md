# Thế giới hoạt hình 2D

## Dùng cho
Thế giới trận: hero, quái, thành, nền chiến trường, hiệu ứng đòn đánh và kĩ năng.

## Góc nhìn
2D nhìn ngang từ bên cạnh, mặt đất là một đường ngang. Nhân vật và quái vẽ nghiêng ba phần tư về phía hướng đi; hero và quái mặc định quay phải, game tự lật khi đi sang trái. Thành vẽ chính diện, đối xứng trái phải.

## Bảng màu
- Màu tươi, bão hoà vừa phải.
- Hero dùng tông ấm hoặc sáng; quái dùng tông lạnh hoặc tối hơn hero, để phân biệt phe chỉ bằng màu.
- Nền dịu và tối hơn nhân vật một bậc, để hero và quái nổi lên trên nền.

## Nét viền
- Viền ngoài tối, đều, dày tối thiểu 4 px khi sprite cao 256 px.
- Chi tiết bên trong viền mảnh hơn viền ngoài.

## Độ chi tiết
- Tỉ lệ đầu to, thân ngắn (khoảng 1:2 đầu so với thân), dáng đọc được khi chỉ nhìn bóng đen.
- Mỗi hero và mỗi loại quái có một nét nhận dạng lớn (vũ khí, mũ, hình khối) thấy rõ khi sprite cao 128 px.
- Mỗi nhân vật là một hình đứng yên; chuyển động do game tạo bằng scale, move, rotate hoặc đổi màu, không cần khung hình.

## Ánh sáng
Một nguồn sáng từ trên bên trái. Đổ bóng thành 2 bậc (sáng và tối), không chuyển mềm. Bóng đổ dưới chân là một hình bầu dục tối, không vẽ vào sprite.

## Làm
- Sprite nền trong suốt, mỗi file một nhân vật hoặc một vật.
- Thành, hero và quái vẽ cùng một tỉ lệ để đặt cạnh nhau không lệch cỡ.
- Hiệu ứng (sấm, băng, nổ) là hình khối rõ, có viền, đọc được trong một khoảnh khắc.

## Không làm
- Không vẽ kiểu thật, không phối cảnh 3D, không nhìn từ trên xuống.
- Không gradient mịn, không texture chi tiết.
- Không viền mờ hay viền màu nhạt hơn thân.
- Không vẽ nền phía sau nhân vật trong cùng một file.
