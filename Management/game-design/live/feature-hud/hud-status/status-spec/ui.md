## Phần tử
- hp | thanh | chính | máu thành theo tỉ lệ hiện tại trên tối đa
- hp-number | nhãn | chính | chữ máu "hiện tại/tối đa", ví dụ "750/1000", nằm trên thanh máu
- coin | icon | phụ | biểu tượng xu, đại diện cho tiền trong lượt
- money | nhãn | chính | số tiền hiện có, số nguyên đầy đủ
- wave | nhãn | phụ | chữ "Đợt N": đợt đang đánh, trong lúc thở là đợt sắp tới
- banner | nhãn | phụ | thông báo "Đợt N" lúc một đợt bắt đầu | trạng thái: ẩn, đang hiện (1.5 giây)

## Tương tác
- hp, hp-number, coin, money, wave: không nhận chạm.
- banner: không nhận chạm, chạm đi xuyên tới phần bên dưới.

## Flow
- Trận dựng xong → hiện 1000/1000, 50, "Đợt 1"; banner ẩn.
- Đợt bắt đầu → banner hiện "Đợt N" 1.5 giây rồi ẩn; đợt mới bắt đầu khi banner còn hiện thì thay bằng banner mới.
- Lượt kết thúc → giữ giá trị cuối, banner tắt; popup thua chồng lên.
