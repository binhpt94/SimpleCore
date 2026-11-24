# Nội dung và nút của popup hồi sinh

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lượt có nhiều nhất MỘT popup hồi sinh.
- Một popup gửi nhiều nhất MỘT yêu cầu quảng cáo (`feature-ads`).
- Một popup báo `feature-run` đúng MỘT lựa chọn cuối: hồi sinh, không hồi sinh, hoặc quảng cáo không xem được.
- Popup hiện qua dịch vụ `feature-popup`, nằm trên HUD và trận.

## Định nghĩa
- Không dùng thuật ngữ riêng.

## Danh sách
Trên popup:
- Tiêu đề (nói thành vừa bị phá).
- Dòng hồi sinh được gì (thành có lại một phần máu, trận chạy tiếp từ chỗ dừng).
- Nút "Xem quảng cáo" (chính).
- Nút "Kết thúc lượt".
- Dòng đang chờ quảng cáo (chỉ ở trạng thái chờ quảng cáo).

| Trạng thái | Nút | Thoát ra khi |
|---|---|---|
| Chờ chọn | Hai nút bấm được | Chạm một nút |
| Chờ quảng cáo | Hai nút khoá, hiện dòng đang chờ | `feature-ads` trả kết quả |

## Luật cơ bản
- Đóng popup trước, báo `feature-run` sau.
- Không đóng bằng chạm ngoài hay nút quay lại.
- Một popup chỉ một yêu cầu quảng cáo.

## Đã tự quyết
- **"Kết thúc lượt" thay cho "Bỏ qua".** Theo góp ý GPT ở vòng 2 (`revive-offer`): người chơi biết bấm là thua.
- **Không có đồng hồ tự đóng.** Trận đã dừng; người chơi chủ động chọn (vòng 2).
- **Khoá cả hai nút khi chờ quảng cáo.** Tránh chốt thua đúng lúc quảng cáo trả "đã xem đủ".

## Chưa rõ
- Chữ cụ thể của tiêu đề và dòng hồi sinh, cỡ popup: vòng 4.
