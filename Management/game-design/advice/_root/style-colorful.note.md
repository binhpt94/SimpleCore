# Nhận xét cho style-colorful (_root)

Nhận xét cho người chấm, không phải lí do để bỏ góp ý.

- GPT không xem được ảnh. Câu hỏi có kèm mô tả ảnh do @DesignAdvisor viết; góp ý đối chiếu với mô tả đó.
- Ba góp ý có con số: `do-tuong-phan-hud` (4,5:1), `nen-nhuong-vien-don-vi` (3:1), `gioi-han-che-boi-hieu-ung` (50%). Luật vòng 1 bỏ con số khỏi `data.md` của gốc, nhưng file style `.art-style/*.md` đã có con số px. Nên đưa ba quy tắc này vào file style, hoặc giữ ý ở gốc và để số cho vòng 4 (`feature-hud`, `feature-battlefield`, nhánh hiệu ứng).
- `canh-tran-mau` (khung trận mẫu đã duyệt): hiện chưa có khung trận mẫu. Tương ứng gần nhất là frame và claude-concept của `feature-battlefield` cùng HUD; cần chọn khung nào làm mốc.
- `mau-nut-quang-cao`: khớp quy tắc "nút có quảng cáo hoặc hồi sinh: hồng" trong `ui-flat`, chỉ nói rõ thêm trường hợp nút quảng cáo là nút chính (popup hồi sinh).
- `chu-dat-trong-ui` (font có đủ dấu tiếng Việt): ngôn ngữ hiển thị của game chưa ghi ở gốc. Ảnh mẫu dùng chữ tiếng Anh.
- `anh-sang-tu-tren`: `world-cartoon` đang ghi nguồn sáng "từ trên bên trái", còn `ui-flat` ghi "từ trên". Góp ý này gom về một hướng; cần chọn có giữ "bên trái" cho thế giới không.
