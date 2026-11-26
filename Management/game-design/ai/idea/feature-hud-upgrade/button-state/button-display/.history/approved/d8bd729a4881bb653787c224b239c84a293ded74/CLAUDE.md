# Hiển thị và chạm nút nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Mỗi nút nhận từ `feature-upgrade/upgrade-purchase/purchase-rules`: cấp hiện tại, giá cấp sau, mua được hay không. Nhận lại mỗi khi một trong ba thứ đổi, và hiện ngay trong khung hình đó.
- Mỗi nút hiện:
  | Thứ | Nội dung | Ví dụ |
  |---|---|---|
  | Hình | Chân dung hero ở chỗ đó, hoặc biểu tượng mục thành | Chân dung hero chỗ 2 |
  | Cấp | "Cấp N", N là cấp hiện tại | "Cấp 3" |
  | Giá | Giá cấp sau kèm biểu tượng tiền, rút gọn khi lớn | 55, "12,3K" |
- Hình của mục:
  | Nút | Hình đại diện cho |
  |---|---|
  | Hero 1, 2, 3 | Chính hero ở chỗ đó trong đội |
  | Máu | Máu thành |
  | Sức đánh | Đòn tự bắn của thành |
  | Kĩ năng | Cả ba kĩ năng thành gộp chung |
- Trạng thái: sáng (mua được) hoặc mờ (không mua được: không đủ tiền hoặc lượt đã kết thúc).
- Chạm nút sáng: gửi một yêu cầu mua cho mục đó tới `purchase-rules`. Chạm nút mờ: không gửi gì, không có phản hồi.
- Mua thành công: nút nảy một lần (phóng to rồi về cỡ cũ) trong 0,2 giây; cấp và giá đổi theo thông tin mới.
- Nút không tự tính giá, không tự so tiền với giá, không tự quyết mua.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số thứ hiện trên mỗi nút | 3 | | Hình, cấp, giá (vòng 3) |
| Số trạng thái | 2 | | Sáng, mờ (vòng 3) |
| Độ trễ cập nhật | 0 | khung hình | Mua giữa trận cần thấy đúng ngay |
| Thời gian nảy khi mua thành công | 0,2 (dự kiến) | giây | Đủ thấy, không cản chạm tiếp |
| Ngưỡng rút gọn số | 10.000 | tiền | Giữ chữ ngắn trên nút; giá dưới ngưỡng đọc dễ |
| Rút gọn | 10.000-999.999 → "N,NK"; >= 1.000.000 → "N,NM"; cắt bớt một chữ số thập phân | | Ví dụ 12.399 → "12,3K"; 1.250.000 → "1,2M" |
| Số yêu cầu mua mỗi chạm nút sáng | 1 | yêu cầu | Vòng 3 |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Hình hero | sprite hero gen bằng AI, thu nhỏ | chân dung riêng gen bằng AI | chân dung đủ art |
| Biểu tượng máu / sức đánh / kĩ năng | hình đơn giản gen bằng AI | biểu tượng riêng | đủ art |
| Phản hồi mua thành công | nảy 0,2 giây | nảy 0,2 giây và số cấp mới nhấp sáng 0,2 giây; không có tiếng | thêm tiếng mua và hiệu ứng đủ art |
| Phản hồi chạm nút mờ | không | rung nhẹ nút 0,2 giây | rung và tiếng báo không đủ tiền |
| Dấu "hero đang gục" trên nút hero | không | có | có |
| Rút gọn số | có | có | có |

## Trường hợp biên
- **Bấm hai lần rất nhanh khi chỉ đủ tiền một lần**: nút gửi hai yêu cầu (nếu chạm lần hai tới trước khi nút kịp mờ) hoặc một yêu cầu; `purchase-rules` xử lí lần lượt nên chỉ một lần mua thành công, nút nảy một lần, rồi mờ.
- **Số dư vừa đúng bằng giá**: nút sáng; mua xong số dư về 0, nút mờ ngay (giá mới cao hơn).
- **Hero đang gục**: nút hero vẫn sáng nếu đủ tiền, mua được.
- **Thua đúng lúc đang chạm**: từ khung hình thành về 0 máu mọi nút mờ; chạm không gửi yêu cầu hoặc yêu cầu bị từ chối.
- **Giá rất lớn**: rút gọn ("181,0K"); sáng/mờ theo giá thật, nên có thể thấy giá "12,3K" mờ khi đang có 12.350 tiền (giá thật 12.399).
- **Cấp rất lớn**: hiện đủ số, không rút gọn (cấp không lớn tới mức cần).
- **Tiền vào làm nhiều nút cùng sáng**: mọi nút đổi trong cùng khung hình.

## Đã tự quyết
- **Ngưỡng rút gọn 10.000.** Giá dưới 10.000 là phần lớn trận; trên đó chữ dài khó đọc trên nút. Đã loại: rút gọn từ 1.000 (mất độ chính xác khi giá còn thường gặp).
- **Cắt bớt, không làm tròn lên.** Đơn giản, quen thuộc; sáng/mờ đã nói đúng mua được hay không. Đã loại: làm tròn lên.
- **Không phản hồi khi chạm nút mờ.** Đủ cho chơi thử; phản hồi "không đủ tiền" để sau theo bảng giai đoạn.
- **Không có tiếng ở PROTOTYPE (2026-11-26, người quyết qua @Manager: không dùng âm thanh, không bật module Audio ở PROTOTYPE).** Tiếng mua dời sang FULL; bù bằng hình: nút nảy và số cấp mới nhấp sáng 0,2 giây, để người chơi thấy rõ món vừa mua. Tiếng báo không đủ tiền vốn đã ở FULL.
- **Dùng dấu phẩy thập phân kiểu Việt ("12,3K").** Khớp ngôn ngữ game. Đã loại: "12.3K".
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ tạm dừng khỏi lí do nút mờ và trường hợp biên tạm dừng.

## Chưa rõ
- Đề xuất cho người: `feature-hud` hiện số tiền nên dùng cùng luật rút gọn để người chơi so được số tiền và giá.
- Ngôn ngữ game (tiếng Việt hay tiếng Anh) chưa chốt; nếu tiếng Anh thì đổi "Cấp N" thành "Lv N" và dấu thập phân thành dấu chấm.
