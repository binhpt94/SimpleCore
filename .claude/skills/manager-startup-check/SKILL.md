---
name: manager-startup-check
description: Chỉ phiên FEATURE=@Manager. Kiểm tra đầu phiên ba bước đúng thứ tự - Leader có live không (ListAgents), tiến độ ba Leader (gd_status, gd_req.py status, báo cáo, hỏi từng Leader), bản tổng hợp cho người. Tầng dưới là của CodeLead, không rà ở đây. Dùng ở lượt làm việc đầu tiên sau khi phiên được tạo mới hoặc renew, trước mọi việc khác.
---
# @Manager: kiểm tra đầu phiên
Luật chung của @Manager (phạm vi chỉ ba Leader, quyền, chuỗi của một feature, giao việc) vẫn ở `.claude/rules/manager.md`.

Chạy ở **lượt làm việc đầu tiên sau khi phiên được tạo mới hoặc renew** (tin "vừa được làm mới" hoặc tin tạo thread chỉ yêu cầu đọc luật; kiểm tra chạy ở tin kế tiếp), **trước mọi việc điều phối khác**, kể cả khi người giao ngay một việc cụ thể. Làm đúng thứ tự, không nhảy bước.

## Bước 1. Leader có live không
- Gọi `ListAgents`. Phải thấy cả `CodeLead`, `GameDesigner` và `UIDesigner`.
- **Thiếu một trong ba: báo lỗi cho người và dừng hẳn.** Tin báo nêu Leader nào không live và cách mở lại (tab Setup của trang quản lý). Không nhắn phiên nào, không làm bước 2, 3, không làm việc người vừa giao.
- Không tự tạo, tự gán hay tự renew thread: đó là việc của người.

## Bước 1b. Giai đoạn đang làm
- Đọc dòng "Giai đoạn đang làm" ở dòng `guard:` đầu tin (hoặc `python Management/tools/gd_req.py status`).
- **Đã đặt:** trong bản tổng hợp cho người (bước 3), nêu giai đoạn đang ghi và hỏi người xác nhận còn đúng không. Người trả lời rồi thì nhắn `CodeLead` một tin: giai đoạn đã xác nhận. CodeLead giao việc dev theo đó.
- **Chưa đặt:** hỏi người đang làm giai đoạn nào (IDEA, PROTOTYPE hay FULL) và nhờ người đặt ở tab Game design, nút "Đang làm". Guard chặn việc dev của CodeLead cho tới khi người đặt; không tìm đường vòng.
- Bạn không tự đặt, không tự suy ra giai đoạn từ tiến độ.

## Bước 2. Tiến độ của ba Leader
Chỉ làm khi bước 1 qua. Một lần gọi shell được nối tối đa 6 lệnh: `python Management/tools/gd_status.py --brief; python Management/tools/gd_req.py status; python Management/tools/toolwork.py list`.
- **@GameDesigner**: từ `gd_status.py` (cả cây), ghi lại:
  - mode và vòng đang làm;
  - node chưa đạt, node chưa hỏi GPT, node có xung đột chưa giải quyết (khoá vòng sau);
  - mục "Cổng code theo feature" ở đầu bản in: feature nào ĐƯỢC CODE, feature nào ĐANG KHOÁ và vì sao.

  Đọc báo cáo mới của nó (đầu tin liệt kê). Rồi nhắn `GameDesigner`: đang làm gì, đang chờ người việc gì.
- **@UIDesigner**: trong bản in `gd_status.py` ở trên, đếm dòng `CHỜ @UIDesigner` (frame thiếu, cũ, thiếu phần tử chính, bị chấm thấp); đọc báo cáo mới của nó. Rồi nhắn `UIDesigner`: đang xếp node nào, đang chờ người chấm frame nào.
- **@CodeLead**:
  - Từ `gd_req.py status` (một dòng mỗi feature), ghi đã kiểm/tổng, chờ kiểm, bị trả, đổi thiết kế. Feature ĐƯỢC CODE mà chưa có yêu cầu nào đã làm thì đánh dấu chưa giao.
  - Đọc báo cáo mới của CodeLead.
  - Rồi nhắn `CodeLead`: khung, nối dây, test, đợt chơi thử đang ở đâu; tầng dưới đang bị chặn gì; có cần người quyết gì không.
  - Không rà từng feature, tool hay tester: CodeLead trả lời thay cho tầng dưới.
- **Hộp thư** (chỉ một việc): dòng `guard:` báo SERVER TẮT thì đưa "bật setup_server.py" lên đầu bản tổng hợp.
- Leader chưa trả lời trong lượt đó: ghi "chưa trả lời" vào bản tổng hợp. Không coi là xong, không tự suy ra tiến độ thay Leader.

## Bước 3. Bản tổng hợp cho người
Một tin, theo thứ tự:
1. Ba Leader: live, tiến độ, đang chờ gì.
2. Tiến độ code tóm từ `gd_req.py status` và câu trả lời của CodeLead.
3. Việc bị chặn và ai gỡ được.
4. Việc công cụ đang chờ phiên `@human` (`toolwork.py list`).
5. Các câu đang chờ người quyết (khuôn ở `manager.md`, mục "Hỏi người"), cùng câu hỏi xác nhận giai đoạn.
6. Bước tiếp bạn đề nghị.

Sau tin này mới làm việc người giao và điều phối tiếp, theo skill `manager-dispatch`.
