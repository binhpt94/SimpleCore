# Độ khó theo đợt

Dữ liệu cốt lõi (tóm tắt và các ý có điểm): [data.md](data.md)

## Người chơi làm gì
- Không làm gì trực tiếp. Người chơi cảm nhận độ khó và phản ứng bằng nâng cấp ở `feature-upgrade`.

## Người chơi thấy gì
- Đợt sau đông hơn, quái lì đòn hơn.
- Khi đợt sắp tới có loại quái lần đầu xuất hiện, màn nghỉ báo trước (xem `feature-wave/break-phase`).

## Luật
- Số quái của đợt N = số quái gốc + số tăng mỗi đợt × (N − 1), không vượt mức trần.
- Máu quái đợt N = máu gốc của loại đó × (1 + tỉ lệ tăng máu × (N − 1)).
- Sức đánh quái đợt N = sức đánh gốc × (1 + tỉ lệ tăng sức đánh × (N − 1)).
- Tốc độ đi và tốc độ đánh của quái không tăng theo đợt (giữ quái nào trông ra quái nấy).
- Tiền rơi của quái đợt N = tiền gốc × (1 + tỉ lệ tăng tiền × (N − 1)), để thu nhập theo kịp giá nâng cấp tăng dần.
- Loại quái mở dần theo đợt; mỗi loại chiếm một tỉ lệ trong đợt, phần còn lại là quái thường.
- Chỉ số gốc của từng loại quái nằm ở `feature-enemy`; node này chỉ quyết hệ số nhân theo đợt.
- Mọi con số dưới đây là tham số chỉnh được, không viết cứng.

## Trường hợp biên
- Số quái đã chạm trần: đợt sau chỉ còn tăng chỉ số, không tăng số lượng.
- Tỉ lệ loại quái nhân ra số lẻ: làm tròn xuống cho loại đặc biệt, phần dư thành quái thường; nếu loại đã mở mà làm tròn ra 0 thì vẫn có ít nhất 1 con.
- Đợt rất cao (ví dụ đợt 100): công thức vẫn chạy, không tràn số; chỉ số tăng tuyến tính nên không vọt quá nhanh.

## Con số dự kiến
- Số quái gốc: 6 con; mỗi đợt thêm 2 con; trần 60 con (đạt ở đợt 28). Đợt 1 ngắn để làm quen, khoảng 30 giây.
- Tỉ lệ tăng máu: 12% mỗi đợt (đợt 10 gấp 2.08 lần, đợt 20 gấp 3.28 lần).
- Tỉ lệ tăng sức đánh: 8% mỗi đợt (đợt 10 gấp 1.72 lần). Tăng chậm hơn máu để thành không bị phá quá nhanh.
- Tỉ lệ tăng tiền rơi: 5% mỗi đợt.
- Mở loại quái:
  - Đợt 1–2: chỉ quái thường.
  - Từ đợt 3: thêm quái nhanh, chiếm 25% số quái.
  - Từ đợt 5: thêm quái trâu, chiếm 20% số quái.
- Lí do mốc đợt 3 và 5: người chơi đã qua 1–2 lần nâng cấp trước khi gặp loại mới.

## Đã tự quyết
- **Tăng tuyến tính, không tăng theo cấp số nhân.** Dễ cân bằng, dễ đọc; giá nâng cấp tăng dần đã đủ tạo bức tường độ khó. Đã loại: nhân 1.1 mỗi đợt (đợt 30 vọt gấp 17 lần).
- **Không có đợt đặc biệt (đợt mốc, trùm).** `feature-enemy` chưa có trùm. Đã loại: mỗi 5 đợt một đợt đông gấp rưỡi (có thể thêm sau).
- **Tiền rơi tăng theo đợt.** Để không có lúc người chơi kẹt vì không đủ tiền mua gì. Đã loại: tiền rơi cố định.
- **Không có độ ngẫu nhiên trong độ khó.** Cùng số đợt luôn ra cùng số quái và tỉ lệ loại; chỉ thứ tự thả là ngẫu nhiên (`feature-wave/spawning`). Đã loại: số quái dao động ±20%.

## Chưa rõ
- Con số chỉ là dự kiến; cần chơi thử cùng chỉ số hero và giá nâng cấp để chỉnh.
