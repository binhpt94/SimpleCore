# Thủ thành anh hùng (tên tạm)

Dữ liệu cốt lõi (tóm tắt và các ý có điểm): [data.md](data.md)

## Người chơi
- Người chơi phổ thông, chơi lượt ngắn (khoảng 10 đến 20 phút, dự kiến) trên điện thoại cầm ngang.
- Trước trận: chọn đội hero. Trong trận: canh lúc bấm kĩ năng thành, chọn nâng cấp nào mua lúc nào.
- Thích nhìn đội hero của mình mạnh dần.

## Cảm giác chính
- Bị kẹp từ hai phía, áp lực tăng dần qua mỗi đợt.
- Tự tay cứu nguy: bấm kĩ năng thành đúng lúc quái dồn đông.
- Thấy rõ mình mạnh lên ngay trong trận: mua nâng cấp là hero, thành đánh mạnh hơn liền.
- Hồi hộp khi máu thành tụt thấp, nhẹ nhõm khi dọn sạch một đợt.
- Muốn chơi lại để thử đội hero khác và vượt kỉ lục.

## Vòng lặp chơi
1. Trước lượt chơi: chọn đội hero (dự kiến 4 hero từ danh sách 6).
2. Vào trận: cả đội sinh ra ở thành.
3. Đợt quái bắt đầu: quái đi từ mép trái và mép phải vào phía thành.
4. Hero tự đánh quái gần nhất; thành tự bắn quái trong tầm; người chơi bấm kĩ năng thành khi hồi chiêu xong.
5. Hạ quái được tiền; người chơi mua nâng cấp ngay trong trận, trận không dừng.
6. Hero gục thì hồi sinh ở thành sau một lúc. Quái tới thành thì đánh trừ máu thành.
7. Dọn sạch đợt thì sang đợt sau đông hơn, mạnh hơn.
8. Thành còn 0 máu: thua, hiện số đợt đã trụ được và kỉ lục.

## Đã tự quyết
- **Đối tượng: người chơi phổ thông.** Hero tự đánh, thao tác chỉ là bấm kĩ năng và mua nâng cấp. Đã loại: game nặng tính toán, nhiều cây nâng cấp.
- **Quái vào từ hai bên trái và phải.** Màn hình ngang, thành ở giữa, hai bên là hướng tự nhiên. Đã loại: bốn phía.
- **Đội hero cố định, chọn trước lượt chơi (theo phản hồi của người).** Hiểu "mỗi màn" là mỗi lượt chơi, vì game hiện chỉ có một bản đồ và chơi vô tận. Nếu người muốn nhiều màn tách biệt thì cần chấm lại `single-map` và `endless-score`. Đã loại (người loại): thành tự sinh hero theo thời gian.
- **Số hero trong đội: dự kiến 4.** Đủ để mỗi bên thành có hero, màn hình điện thoại vẫn dễ nhìn.
- **Hero gục thì hồi sinh ở thành sau một lúc.** Đội cố định mà chết hẳn thì chơi vô tận sẽ sụp nhanh và mất cảm giác "đội của mình". Đã loại: chết hẳn tới hết lượt.
- **Danh sách hero, mỗi hero chọn một lần; bản đầu dự kiến 6, mở sẵn hết.** Chọn 4 trong 6 đã có lựa chọn đáng nghĩ; không có tiến trình lâu dài nên không có mở khoá. Đã loại: mở loại hero bằng nâng cấp trong lượt (không hợp với chọn đội trước).
- **Thành tự đánh và có kĩ năng người chơi bấm (theo phản hồi của người).** Dự kiến 1 đến 2 kĩ năng cho bản đầu; chi tiết ở vòng sau. Đã loại (người loại): thành không đánh.
- **Nâng cấp ngay trong trận, trận không dừng (theo phản hồi của người).** Đã loại (người loại): chỉ nâng cấp giữa hai đợt.
- **Nâng cấp thành gồm máu, sức đánh, kĩ năng.** Bỏ tốc độ sinh hero và giới hạn số hero vì đội giờ cố định.
- **Mỗi lượt chơi bắt đầu lại từ đầu.** Ý tưởng gốc chưa nhắc tới tiến trình lâu dài. Chỉ lưu kỉ lục. Đã loại: nâng cấp vĩnh viễn giữa các lượt.
- **Chơi vô tận tới khi thua; điểm là số đợt trụ được.** Điều kiện kết thúc duy nhất trong ý tưởng là thành hết máu. Đã loại: thắng khi qua đủ N đợt.
- **Một bản đồ duy nhất.** Giữ phạm vi nhỏ cho bản đầu. Đã loại: nhiều màn.

## Chưa rõ
- Tên chính thức và phong cách hình ảnh: chưa có, không chặn việc chia feature.
