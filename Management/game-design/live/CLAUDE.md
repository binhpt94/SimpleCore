# Thủ thành anh hùng (tên tạm)

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi
- Người chơi phổ thông, chơi lượt ngắn trên điện thoại cầm ngang.
- Không cần nhanh tay. Trước trận chọn đội hero; trong trận canh lúc bấm kĩ năng thành và chọn nâng cấp nào mua lúc nào.
- Thích nhìn đội hero của mình mạnh dần và thử đội khác ở lượt sau.

## Cảm giác chính
- Bị kẹp từ hai phía, áp lực tăng dần qua mỗi đợt.
- Tự tay cứu nguy: bấm kĩ năng thành đúng lúc quái dồn đông.
- Thấy rõ mình mạnh lên ngay trong trận: vừa mua nâng cấp là hero, thành đánh mạnh hơn liền.
- Hồi hộp khi thành sắp bị phá, nhẹ nhõm khi dọn sạch một đợt.

## Vòng lặp chơi
1. **Làm gì:** chọn đội hero, vào trận; đội tự đánh quái từ hai bên, người chơi bấm kĩ năng thành và mua nâng cấp.
2. **Được gì:** hạ quái được tiền; tiền đổi ra nâng cấp; dọn sạch đợt thì điểm tăng.
3. **Lặp lại thế nào:** đợt sau khó hơn, phải mua thêm nâng cấp để trụ, tới khi thành bị phá. Thua thì xem điểm và kỉ lục, chọn đội khác, chơi lại từ đầu.

Kể trong một phút: một tòa thành đứng giữa màn hình, quái kéo tới từ cả hai bên. Bạn chọn trước một đội hero; vào trận, hero tự lao ra đánh, thành tự bắn. Việc của bạn: canh lúc bấm kĩ năng của thành và tiêu tiền hạ quái vào nâng cấp, ngay giữa trận. Đợt sau đông hơn, mạnh hơn. Thành bị phá là hết lượt; trụ được càng nhiều đợt càng cao điểm.

Khác game thủ thành thường gặp (xây tháp dọc đường, hoặc điều khiển hero): không xây, không điều khiển; bị kẹp hai phía; quyết định nằm ở chọn đội, canh kĩ năng và chi tiền.

## Đã tự quyết
- **Đối tượng: người chơi phổ thông.** Hero tự đánh, thao tác chỉ là bấm kĩ năng và mua nâng cấp. Đã loại: game nặng tính toán.
- **Nhìn ngang từ bên cạnh, thành ở giữa.** "Màn hình ngang" trong ý tưởng gốc hiểu là góc nhìn ngang trên điện thoại cầm ngang. Đã loại: nhìn từ trên xuống.
- **Quái vào từ hai bên trái và phải.** Thành ở giữa thì hai bên là hướng tự nhiên. Đã loại: bốn phía, một phía.
- **Đội hero cố định, chọn trước lượt chơi (theo phản hồi của người).** Hiểu "mỗi màn" là mỗi lượt chơi, vì game có một bản đồ và chơi vô tận. Nếu người muốn nhiều màn tách biệt thì cần chấm lại `single-map` và `endless-score`. Đã loại (người loại): thành tự sinh hero theo thời gian.
- **Hero gục thì hồi sinh ở thành.** Đội cố định mà chết hẳn thì chơi vô tận sẽ sụp nhanh và mất cảm giác "đội của mình". Đã loại: chết hẳn tới hết lượt.
- **Nhiều hero hơn số chỗ trong đội, chọn được hết ngay.** Thừa hero so với chỗ là đã có lựa chọn đáng nghĩ; không có tiến trình lâu dài nên không mở khoá.
- **Thành tự đánh và có kĩ năng người chơi bấm (theo phản hồi của người).** Đã loại (người loại): thành không đánh.
- **Nâng cấp ngay trong trận, trận không dừng (theo phản hồi của người).** Đã loại (người loại): chỉ nâng cấp giữa hai đợt. Giữ `id` `upgrade-between-waves` theo quy trình dù tên không còn khớp.
- **Mỗi lượt bắt đầu lại từ đầu, chỉ lưu kỉ lục.** Ý tưởng gốc chưa nhắc tới tiến trình lâu dài. Đã loại: nâng cấp vĩnh viễn giữa các lượt.
- **Không có thắng, chơi vô tận.** Điều kiện kết thúc duy nhất trong ý tưởng là thành bị phá. Đã loại: thắng khi qua đủ một số đợt.
- **Một bản đồ duy nhất.** Giữ game gọn. Đã loại: nhiều màn.
- **Làm lại theo luật vòng 1 mới (2026-11-23):**
  - Thêm góc nhìn và điểm khác với game cùng thể loại (tóm tắt, mục "Kể trong một phút").
  - Bỏ mọi con số (thời lượng lượt, số hero trong đội, số hero trong danh sách); để vòng 4.
  - Bỏ chi tiết mức định nghĩa và danh sách: "0 máu" thành "thành bị phá"; "hero đánh quái gần nhất", "thành bắn quái trong tầm", danh sách nâng cấp của thành (máu, sức đánh, kĩ năng) chuyển xuống các vòng sau.
  - Không dùng chữ theo bản phát hành ("bản đầu"), theo yêu cầu của người.

## Chưa rõ
- Tên chính thức và phong cách hình ảnh (2D hay 3D low poly): chưa có, không chặn việc chia feature.
