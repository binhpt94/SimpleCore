# Báo cáo cho @Manager
Áp dụng cho phiên feature, dịch vụ, @Core, @Composition. @human và @Manager không phải báo.

## Khi nào
- Lượt nào có sửa file (Edit, Write, xoá qua unity-mcp) thì kết thúc câu trả lời bằng khối báo cáo bên dưới.
- Dở việc hoặc bị chặn cũng báo, ghi đúng trạng thái.
- Thiếu khối thì Stop hook (`stop_report.py`) bắt viết lại. Dừng lần nữa mà vẫn thiếu thì Manager nhận ghi chú "phiên dừng mà không báo cáo".

## Format
Đủ 8 dòng, đúng tên, mỗi dòng có nội dung:

```
## Báo cáo cho @Manager
- Phiên: FEATURE=<giá trị của chat này>
- Việc: <1 câu: đã làm gì>
- Trạng thái: xong | dở | bị chặn
- Đã đổi: <file hoặc vùng>
- Công khai đổi: không | <Contracts, FeatureAssets/Public, API của Core... đã đổi>
- Cần bên khác: không | <phiên nào, việc gì>
- Kiểm tra: <kết quả validate.py; test cần người chạy>
- Ghi chú: <rủi ro, câu hỏi cho người; hoặc không>
```

- "Công khai đổi" và "Cần bên khác" là thứ Manager dùng để điều phối: ghi rõ tên feature, contract, id asset.
- Không ghi PIN hay nội dung bí mật.

## Đi đâu
- Hook lưu khối vào `Management/reports/<thời gian>-<vai trò>.md` (không commit).
- Phiên @Manager thấy danh sách báo cáo mới ở đầu mỗi tin; trang quản lý có tab Báo cáo.
- @Manager điều phối bằng SendMessage tới các phiên và đề xuất trong `Management/proposals`; không sửa code.
