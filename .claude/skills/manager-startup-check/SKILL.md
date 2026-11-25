---
name: manager-startup-check
description: Chỉ phiên FEATURE=@Manager. Kiểm tra đầu phiên bốn bước đúng thứ tự - Leader có live không (ListAgents), tiến độ ba Leader, các tầng dưới (Tools, Testers, Basic, Services, Features), bản tổng hợp cho người. Dùng ở lượt làm việc đầu tiên sau khi phiên được tạo mới hoặc renew, trước mọi việc khác.
---
# @Manager: kiểm tra đầu phiên
Luật chung của @Manager (quyền, chuỗi của một feature, chạy song song, giao việc) vẫn ở `.claude/rules/manager.md`.

Chạy ở **lượt làm việc đầu tiên sau khi phiên được tạo mới hoặc renew** (tin "vừa được làm mới" hoặc tin tạo thread chỉ yêu cầu đọc luật; kiểm tra chạy ở tin kế tiếp), **trước mọi việc điều phối khác**, kể cả khi người giao ngay một việc cụ thể. Làm đúng thứ tự, không nhảy bước.

## Bước 1. Leader có live không
- Gọi `ListAgents`. Phải thấy cả `CodeLead`, `GameDesigner` và `UIDesigner`.
- **Thiếu một trong ba: báo lỗi cho người và dừng hẳn.** Tin báo nêu Leader nào không live và cách mở lại (tab Setup của trang quản lý). Không nhắn phiên nào, không viết đề xuất, không làm bước 2, 3, không làm việc người vừa giao.
- Không tự tạo, tự gán hay tự renew thread: đó là việc của người.

## Bước 1b. Giai đoạn đang làm
- Đọc dòng "Giai đoạn đang làm" ở dòng `guard:` đầu tin (hoặc `python Management/tools/gd_req.py status`).
- **Đã đặt:** trong bản tổng hợp cho người (bước 4), nêu giai đoạn đang ghi và hỏi người xác nhận còn đúng không. Chưa có câu trả lời thì vẫn làm bước 2, 3 và các việc không phải dev (thiết kế, order, báo cáo), nhưng **chưa giao việc dev** cho feature, service, @Core.
- **Chưa đặt:** hỏi người đang làm giai đoạn nào (IDEA, PROTOTYPE hay FULL) và nhờ người đặt ở tab Game design, nút "Đang làm". Guard chặn `dispatch.py send` tới feature, service, @Core cho tới khi người đặt; không tìm đường vòng.
- Bạn không tự đặt, không tự suy ra giai đoạn từ tiến độ.

## Bước 2. Tiến độ của các Leader
Chỉ làm khi bước 1 qua. Xong cả ba Leader rồi mới sang bước 3.
- **@GameDesigner**: chạy `gd_status.py` (cả cây). Ghi lại: mode, vòng đang làm, node chưa đạt, node chưa hỏi GPT, node có xung đột chưa giải quyết (khoá vòng sau), và mục "Cổng code theo feature" ở đầu bản in (feature nào ĐƯỢC CODE, feature nào ĐANG KHOÁ và vì sao). Đọc các báo cáo mới của @GameDesigner ở `Management/reports/` (nó báo cáo sau mỗi lượt có sửa thiết kế, như các phiên code). Rồi nhắn `GameDesigner`: đang làm gì, đang chờ người việc gì.
- **@UIDesigner**: trong bản in `gd_status.py` ở trên, đếm dòng `CHỜ @UIDesigner` (frame thiếu, cũ, thiếu phần tử chính, bị chấm thấp); đọc báo cáo mới của nó. Rồi nhắn `UIDesigner`: đang xếp node nào, đang chờ người chấm frame nào.
- **@CodeLead**: đọc các báo cáo mới ở `Management/reports/`. Rồi nhắn `CodeLead`: khung, nối dây, test đang ở đâu; đang chờ phiên nào sửa gì.
- Leader chưa trả lời trong lượt đó: ghi "chưa trả lời" vào bản tổng hợp. Không coi là xong, không tự suy ra tiến độ thay Leader, và không giao việc cho tầng dưới thuộc phần của Leader đó (thiết kế: @GameDesigner; bố cục: @UIDesigner; code: @CodeLead) cho tới khi có trả lời.

## Bước 3. Các tầng nhỏ hơn, từ trên xuống
Tools → Testers → Basic → Services → Features. Feature, service, @Core và tool chạy theo việc: không có trong `ListAgents` khi rảnh là bình thường, giao việc bằng hộp thư (`dispatch.py send`), không SendMessage. Tester thì phải live mới nhắn được; không live thì ghi lại, không dừng.
- **Hộp thư**: `python Management/tools/dispatch.py list` (server có đang giao tin không, tin chờ, tin lỗi). SERVER TẮT có tin chờ: đưa "bật setup_server.py" lên đầu bản tổng hợp; tin `error`: xem lỗi, thử lại trên tab Hộp thư hoặc báo người.
- **Testers**: `python Management/tools/tester.py status` (các đợt chơi thử đang mở, mục đích và kết quả); đợt nào `không đạt` thì lỗi thuộc feature nào, đã có phiên sửa chưa.
- **Tools**: `studio.py status` (order đang chờ, item bị chấm làm lại, phiếu nợ art); lần hỏi GPT còn nợ thì thấy ở `gd_status.py`.
- **Basic, Services, Features**: `python Management/tools/gd_req.py status` (sổ yêu cầu: đã kiểm/tổng, chờ kiểm, bị trả, đổi thiết kế, mất test của từng feature; chờ kiểm thì nhắc `CodeLead`, bị trả, đổi thiết kế, mất test thì giao phiên feature đó qua hộp thư kèm id yêu cầu; `reject` đã tự gửi tin, không gửi lại), báo cáo mới của từng phiên (trạng thái dở, bị chặn, "Cần bên khác"), `validate.py` (vi phạm), feature ĐƯỢC CODE mà chưa có phiên làm (feature ĐANG KHOÁ thì không giao code, xem "Chuỗi của một feature" ở `manager.md`), thư mục còn thiếu (`scaffold.py --dry-run`).

## Bước 4. Bản tổng hợp cho người
Một tin, theo thứ tự tầng: Leader (live, tiến độ, đang chờ gì) → Tools → Testers → Basic → Services → Features → việc bị chặn và ai gỡ được → đề xuất bước tiếp. Sau tin này mới làm việc người giao và điều phối tiếp, theo skill `manager-dispatch`.
