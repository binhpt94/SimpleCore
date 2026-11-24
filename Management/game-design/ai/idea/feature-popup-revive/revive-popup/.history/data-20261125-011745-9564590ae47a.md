---
based-on: 53c6e52aecb5
---
# Nội dung và nút của popup hồi sinh

Popup hồi sinh có tiêu đề, một dòng nói hồi sinh được gì, nút xem quảng cáo và nút kết thúc lượt. Popup có hai trạng thái: chờ người chơi chọn, và chờ kết quả quảng cáo.

## Ý cốt lõi

### popup-parts | AI 7 | Popup có tiêu đề, một dòng nói thành có lại một phần máu, nút "Xem quảng cáo" và nút "Kết thúc lượt"
"Xem quảng cáo" là nút chính.

### popup-two-states | AI 7 | Popup có hai trạng thái: chờ chọn, và chờ quảng cáo
Trạng thái chờ quảng cáo khoá cả hai nút và hiện dòng đang chờ.

### tap-watch | AI 7 | Chạm "Xem quảng cáo" thì gửi đúng một yêu cầu tới `feature-ads` và sang trạng thái chờ quảng cáo
Chạm thêm khi đang chờ không gửi gì.

### tap-end | AI 7 | Chạm "Kết thúc lượt" thì popup đóng rồi báo `feature-run` là không hồi sinh

### ad-ok | AI 7 | Quảng cáo báo đã xem đủ thì popup đóng rồi báo `feature-run` là hồi sinh

### ad-fail | AI 7 | Quảng cáo báo không xem được thì popup đóng rồi báo `feature-run` là quảng cáo không xem được
`feature-run` chốt thua; popup thua có dòng "Quảng cáo chưa hoàn tất".

### close-only-by-choice | AI 7 | Popup chỉ đóng sau một lựa chọn hoặc một kết quả; chạm ngoài, chạm nền che, nút quay lại của máy không đóng
Đóng mà không chọn thì người chơi kẹt ở trận đã dừng.

### shown-by-service | AI 7 | Popup hiện qua `feature-popup` bằng id riêng, mở do `feature-run` nhờ
Feature này điền chữ và xử lí nút; dịch vụ popup lo hiện, che, đóng.
