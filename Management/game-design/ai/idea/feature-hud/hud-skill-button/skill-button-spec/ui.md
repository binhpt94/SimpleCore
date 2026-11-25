## Phần tử
- thunder | nút | chính | dùng Sấm sét, biểu tượng riêng | trạng thái: sẵn sàng, đang hồi, khoá (tạm dừng hoặc lượt kết thúc)
- frost | nút | chính | dùng Băng giá, biểu tượng riêng | trạng thái: sẵn sàng, đang hồi, khoá (tạm dừng hoặc lượt kết thúc)
- rally | nút | chính | dùng Cổ vũ, biểu tượng riêng | trạng thái: sẵn sàng, đang hồi, khoá (tạm dừng hoặc lượt kết thúc)
- cover | thanh | phụ | lớp phủ trên nút đang hồi, vơi dần theo phần hồi chiêu còn lại
- seconds | nhãn | phụ | số giây hồi chiêu còn lại trên nút đang hồi, làm tròn lên

## Tương tác
- thunder, frost, rally chạm khi sẵn sàng: báo `feature-castle` dùng kĩ năng đó, một lần cho mỗi chạm.
- thunder, frost, rally chạm khi đang hồi hoặc khoá: không có gì xảy ra.
- thunder, frost, rally giữ hoặc kéo: không có thao tác riêng.
- Hồi xong: nút nảy lên 1.2 lần rồi về cỡ cũ trong 0.3 giây, một lần; cover và seconds biến mất.

## Flow
- Không mở màn hay popup nào; kết quả kĩ năng hiện trên chiến trường.
- Popup tạm dừng chồng lên → cả 3 nút khoá, số giây đứng yên; popup đóng → trở lại trạng thái trước.
