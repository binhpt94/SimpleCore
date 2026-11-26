# Ngẫu nhiên theo seed

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lượt có MỘT seed.
- Một seed sinh ra NHIỀU dòng ngẫu nhiên, mỗi feature dùng ngẫu nhiên MỘT dòng.
- Một dòng thuộc MỘT feature, gọi theo tên feature.
- Bên dùng hiện có: `feature-wave` (xáo thứ tự loại quái, gán bên ra).

## Định nghĩa
- `glossary#seed`
- `glossary#run`

## Danh sách
- Feature dùng ngẫu nhiên: `feature-wave`. Feature mới cần ngẫu nhiên thì xin một dòng theo tên của mình.

## Luật cơ bản
- Không feature gameplay nào gọi bộ ngẫu nhiên của engine hay tự tạo bộ ngẫu nhiên.
- Đầu lượt: đặt lại mọi dòng từ seed của lượt.
- Hồi sinh thành trong cùng lượt: các dòng chạy tiếp từ vị trí bốc hiện tại, không đặt lại.
- Huỷ trận: các dòng của lượt bị dọn.
- Chạy lại ra cùng kết quả còn cần cùng đội, config và độ dài từng bước (ý `tester-fast-run` của node feature).
- Phần chỉ để nhìn (hiệu ứng) dùng ngẫu nhiên riêng, không lấy từ các dòng này, để không làm lệch gameplay.

## Đã tự quyết
- **Dòng theo tên feature** (@CodeLead đề nghị). Đã loại: một dòng chung (thêm một lần bốc ở đâu cũng làm lệch cả trận).
- **Đặt lại mỗi lượt** (@CodeLead đề nghị). Chơi lại một lượt từ seed của nó không phụ thuộc các lượt trước.
- **Hiệu ứng không dùng dòng của gameplay.** Hiệu ứng chạy theo khung hình; nếu lấy chung thì số khung khác nhau giữa các máy sẽ làm lệch thứ tự quái.

## Chưa rõ
- Không có điểm chặn. Seed lấy từ đâu ở bản dev và bản phát hành: vòng 4.
