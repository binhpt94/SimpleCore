# Nâng cấp

Dữ liệu cốt lõi (tóm tắt và các ý có điểm): [data.md](data.md)

## Người chơi được gì
- Quyết định chính trong trận: tiêu tiền vào hero nào hay vào thành, và lúc nào.
- Thấy kết quả ngay: bấm là mạnh lên liền, cứu được tình thế đang nguy.

## Cốt lõi
- Thanh nút luôn hiện, bấm một chạm là mua.
- 4 nút hero (một nút cho mỗi hero trong đội) và 3 nút thành, tổng 7 nút (dự kiến, theo đội 4 hero).
- Giá tăng theo cấp, hiệu lực ngay, mất khi hết lượt.

## Liên quan
- `feature-wallet`: trừ tiền.
- `feature-hero`: cấp của từng hero.
- `feature-castle`: máu, sức đánh, kĩ năng của thành.
- `feature-run`: đặt lại mọi cấp khi bắt đầu lượt.

## Đã tự quyết
- **Thanh nút cố định, không mở bảng.** Trận không dừng nên phải mua được bằng một chạm mà không che màn chơi. Đã loại: bảng nâng cấp bật lên (che trận, phải dừng).
- **Mỗi hero một nút tăng mọi chỉ số.** Giữ số nút ít khi đang đánh (7 nút vừa một thanh trên màn hình ngang). Đã loại: nâng từng chỉ số của từng hero (12 nút hero + 3 nút thành, quá nhiều).
- **Danh sách cố định, không bốc thẻ ngẫu nhiên.** Người chơi phổ thông dễ nắm. Đã loại: chọn 1 trong 3 thẻ ngẫu nhiên.
- **Không có cấp tối đa ở vòng này.** Chơi vô tận; cấp tối đa (nếu cần) quyết ở vòng 3.
- **Không bán lại.** Giữ đơn giản.
- **Bỏ mục mở khoá loại hero, tốc độ sinh hero, giới hạn số hero.** Vòng 1 đã đổi sang đội hero chọn trước, cố định.

## Chưa rõ
- Không có điểm chặn.
