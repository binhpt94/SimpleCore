# Báo cáo cho @Manager
Áp dụng cho nhóm Basic (@Core), Features, Services, @CodeLead khi sửa khung hoặc `Assets/_Composition`, @GameDesigner khi sửa cây thiết kế, và nhóm Testers khi ghi một đợt chơi thử. @human, @Manager không phải báo.
Báo cáo đi tới cả @CodeLead (kiểm code, biên dịch, chạy test) và @Manager (điều phối).

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
- Nhóm Testers điền theo đợt chơi thử: "Đã đổi" là đường dẫn đợt (`Tester/<Tên>/<đợt>/`); "Công khai đổi" ghi `không`; "Cần bên khác" là feature có lỗi và lỗi gì; "Kiểm tra" là kết quả `tester.py check` và kết quả đợt (`đạt`, `không đạt`, `dở`).
- @GameDesigner điền theo thiết kế: "Đã đổi" là các node đã thêm, sửa, xoá; "Công khai đổi" là node đã chốt (có trong `live`) mà bản đề xuất vừa đổi, hoặc `không`; "Cần bên khác" là việc đang chờ ai (người: chấm, trả lời, chọn ý trái nhau, chốt; @DesignAdvisor: lần hỏi GPT; @Codex, @ClaudeImage: order); "Kiểm tra" là kết quả `gd_status.py` (node chưa đạt, lỗi cấu trúc).

## Đi đâu
- Hook lưu khối vào `Management/reports/<thời gian>-<vai trò>.md` (không commit).
- Phiên @CodeLead và @Manager mỗi bên thấy danh sách báo cáo mới ở đầu mỗi tin; trang quản lý có tab Báo cáo.
- @CodeLead kiểm code rồi nhắn lỗi về đúng phiên; phiên đó tự sửa.
- @Manager điều phối bằng SendMessage tới các phiên và đề xuất trong `Management/proposals`; không sửa code.
