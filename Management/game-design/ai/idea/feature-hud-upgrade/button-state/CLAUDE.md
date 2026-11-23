# Trạng thái nút nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT nút hiện thông tin của MỘT mục (`feature-upgrade/upgrade-list`).
- Cấp hiện tại, giá cấp sau, mua được hay không: lấy từ `feature-upgrade/upgrade-purchase`.
- MỘT chạm vào nút sáng là MỘT yêu cầu mua gửi tới `feature-upgrade/upgrade-purchase`.
- Vị trí nút trên thanh: `feature-hud-upgrade/bar-layout`.

## Danh sách
- Thứ mỗi nút hiện: hình của mục, cấp hiện tại, giá cấp sau, sáng hoặc mờ.
- Hai trạng thái của nút: sáng, mờ.

## Luật cơ bản
- Thông tin trên nút đổi ngay khi cấp hoặc số dư đổi.
- Chạm nút sáng: gửi yêu cầu mua; mua xong thì cấp và giá trên nút đổi sang cấp mới.
- Chạm nút mờ: không gửi yêu cầu mua.
- Nút không tự tính hay quyết định gì.

## Đã tự quyết
- **Mờ chứ không ẩn.** Thanh giữ nguyên chỗ, người chơi biết mục đó vẫn ở đó và thấy giá để dành tiền. Đã loại: ẩn nút.
- **Không xác nhận khi mua.** Trận không dừng, mua phải nhanh; mỗi chạm chỉ một cấp nên lỡ tay cũng không mất nhiều. Đã loại: hỏi lại.
- **Mờ cả lúc tạm dừng.** Khớp luật không mua khi tạm dừng (`feature-upgrade/upgrade-purchase`).
- **Nút không có logic.** Theo luật project: hiển thị không quyết định kết quả.

## Chưa rõ
- Không có điểm chặn.
