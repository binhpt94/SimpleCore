# Báo cáo cho @Manager
Áp dụng cho @CodeLead khi sửa khung hoặc `Assets/_Composition`, @GameDesigner khi sửa cây thiết kế, @UIDesigner khi sửa frame, và việc chơi thử (@UnityAutoPlay) khi ghi một đợt. @human, @Manager không phải báo. Worker không viết khối: server ghi báo cáo của nó (cùng 8 dòng) từ `result.json` và kết quả kiểm.
Báo cáo được lưu thành file và liệt kê ở đầu tin của Leader nắm phần đó (từ 2026-11-26):
- worker, việc chơi thử, và @CodeLead khi sửa khung: tới @CodeLead (kiểm code, giao việc tiếp, xếp lịch chơi thử).
- @CodeLead, @GameDesigner, @UIDesigner: tới @Manager (điều phối ba Leader).

Tên khối vẫn là `## Báo cáo cho @Manager` cho mọi phiên, để hook nhận ra.

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

- "Công khai đổi" và "Cần bên khác" là thứ Manager dùng để điều phối: ghi rõ tên feature, contract, id asset. Câu đang chờ người trả lời thì ghi id câu hỏi (`ask.py`, dạng `q…`).
- Không ghi PIN hay nội dung bí mật.
- Nhóm Testers điền theo đợt chơi thử: "Đã đổi" là đường dẫn đợt (`Tester/<Tên>/<đợt>/`); "Công khai đổi" ghi `không`; "Cần bên khác" là feature có lỗi và lỗi gì; "Kiểm tra" là kết quả `tester.py check` và kết quả đợt (`đạt`, `không đạt`, `dở`).
- @GameDesigner điền theo thiết kế: "Đã đổi" là các node đã thêm, sửa, xoá; "Công khai đổi" là node đã chốt (có trong `live`) mà bản đề xuất vừa đổi, hoặc `không`; "Cần bên khác" là việc đang chờ ai (người: chấm, chọn ý trái nhau, chốt trên trang; câu hỏi cho người: id `ask.py` đã gửi Manager; @DesignAdvisor: lần hỏi GPT; @Codex, @ClaudeImage: order); "Kiểm tra" là kết quả `gd_status.py` (node chưa đạt, lỗi cấu trúc).
- @UIDesigner điền theo bố cục: "Đã đổi" là các frame (`<node>/<tên>.frame`); "Công khai đổi" là node đã có trong `live` mà frame vừa đổi, hoặc `không`; "Cần bên khác" là chờ người chấm frame, chờ @GameDesigner sửa `ui.md`, order claude-concept; "Kiểm tra" là các dòng `CHỜ @UIDesigner` còn lại của `gd_status.py`.

## Đi đâu
- Hook lưu khối vào `Management/reports/<thời gian>-<vai trò>.md` (không commit).
- Đầu mỗi tin, @CodeLead thấy danh sách mọi báo cáo mới, @Manager chỉ thấy báo cáo của ba Leader (và ghi chú của server về họ); trang quản lý có tab Báo cáo.
- @CodeLead kiểm code; lỗi thì giao một việc worker mới cho feature đó (`gd_req.py reject` hay `dispatch.py send`).
- @CodeLead giao việc cho worker và đợt chơi thử bằng hộp thư `dispatch.py send` (feature, service, @Core, @UnityAutoPlay).
- @Manager điều phối ba Leader bằng SendMessage và là phiên duy nhất hỏi người (`manager.md`, mục "Hỏi người"); Leader cần người thì ghi `ask.py` và nhắn Manager. Manager không sửa code, không gửi hộp thư.
