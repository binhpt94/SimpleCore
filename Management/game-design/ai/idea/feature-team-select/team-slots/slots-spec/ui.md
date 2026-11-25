## Phần tử
- title | nhãn | phụ | tên màn "Chọn đội"
- cards | danh sách | chính | 5 thẻ hero để chọn (nội dung thẻ ở `feature-team-select/hero-cards/card-spec`) | trạng thái: chưa chọn, đã chọn
- slots | danh sách | chính | 3 chỗ trong đội, mỗi chỗ hình và tên hero hoặc trống | trạng thái: trống, có hero
- start | nút | chính | vào trận với đội đang chọn | trạng thái: khoá, bấm được, đang chờ dựng trận
- back | nút | phụ | quay về màn bắt đầu | trạng thái: bấm được, khoá khi đang chờ dựng trận
- build-error | nhãn | phụ | dòng "Không vào được trận, thử lại" khi dựng trận lỗi | trạng thái: ẩn, hiện

## Tương tác
- cards chạm thẻ chưa chọn: hero vào chỗ trống số nhỏ nhất; đội đủ 3 thì không đổi
- cards chạm thẻ đã chọn: hero rời đội, chỗ đó trống
- slots chạm chỗ có hero: hero rời đội; chạm chỗ trống: không làm gì
- start chạm khi đủ 3: báo đội cho điều hướng màn một lần, khoá cả màn tới khi có kết quả dựng trận
- start chạm khi thiếu: không làm gì
- back chạm: báo điều hướng màn, bỏ các chỗ đang chọn dở
- nút quay lại của máy: như chạm back

## Flow
- Màn bắt đầu → màn chọn đội (bấm Chơi)
- Màn chọn đội → màn trận (dựng trận xong)
- Màn chọn đội → màn chọn đội có build-error (dựng trận lỗi)
- Màn chọn đội → màn bắt đầu (back)
