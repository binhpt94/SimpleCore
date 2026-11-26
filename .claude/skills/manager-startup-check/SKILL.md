---
name: manager-startup-check
description: Chỉ phiên FEATURE=@Manager. Kiểm tra đầu phiên ba bước đúng thứ tự - Leader có live không (ListAgents), tiến độ ba Leader từ gd_status, gd_req.py status và báo cáo (chỉ hỏi Leader điểm chưa rõ), bản tổng hợp cho người. Tầng dưới là của CodeLead, không rà ở đây. Dùng ở lượt làm việc đầu tiên sau khi phiên được tạo mới hoặc renew, trước mọi việc khác.
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
Chỉ làm khi bước 1 qua. **Tổng hợp từ status và báo cáo, không nhắn Leader để hỏi tiến độ** (người quyết 2026-11-26: mỗi tin hỏi là một lượt của Leader, khoảng 70K token). Một lần gọi: `python Management/tools/gd_status.py --brief; python Management/tools/gd_req.py status; python Management/tools/toolwork.py list`.
- **@GameDesigner**: từ `gd_status.py` (cả cây), ghi lại:
  - mode và vòng đang làm;
  - node chưa đạt, node chưa hỏi GPT, node có xung đột chưa giải quyết (khoá vòng sau);
  - mục "Cổng code theo feature" ở đầu bản in: feature nào ĐƯỢC CODE, feature nào ĐANG KHOÁ và vì sao.

  Cộng thêm báo cáo mới của nó (đầu tin liệt kê).
- **@UIDesigner**: trong bản in `gd_status.py` ở trên, đếm dòng `CHỜ @UIDesigner` (frame thiếu, cũ, thiếu phần tử chính, bị chấm thấp). Cộng thêm báo cáo mới của nó.
- **@CodeLead**:
  - Từ `gd_req.py status` (một dòng mỗi feature), ghi đã kiểm/tổng, chờ kiểm, bị trả, đổi thiết kế. Feature ĐƯỢC CODE mà chưa có yêu cầu nào đã làm thì đánh dấu chưa giao.
  - Cộng thêm báo cáo mới của CodeLead (khung, nối dây, đợt chơi thử, tầng dưới bị chặn).
  - Không rà từng feature, tool hay tester.
- **Chỉ hỏi khi thật cần:** nhắn một Leader khi có một điểm cụ thể mà status và báo cáo không trả lời được, ví dụ có việc chờ kiểm hay bị trả nhưng không có báo cáo nào từ lần giao trước, hoặc báo cáo nói đang chặn mà không nêu chặn gì. Mỗi Leader nhiều nhất một tin trong lượt, chỉ nêu đúng câu cần hỏi. Không có điểm nào như vậy thì không nhắn.
- **Hộp thư** (chỉ một việc): dòng `guard:` báo SERVER TẮT thì đưa "bật setup_server.py" lên đầu bản tổng hợp.
- Trong bản tổng hợp, ghi nguồn của từng Leader: "theo status/báo cáo", hoặc "đã hỏi" kèm câu trả lời, hoặc "đã hỏi, chưa trả lời". Không tự suy ra điều status và báo cáo không nói.

## Bước 3. Bản tổng hợp cho người
Một tin, theo thứ tự:
1. Ba Leader: live, tiến độ, đang chờ gì.
2. Tiến độ code tóm từ `gd_req.py status` và báo cáo của CodeLead.
3. Việc bị chặn và ai gỡ được.
4. Việc công cụ đang chờ phiên `@human` (`toolwork.py list`).
5. Các câu đang chờ người quyết (khuôn ở `manager.md`, mục "Hỏi người"), cùng câu hỏi xác nhận giai đoạn và **câu hỏi phạm vi việc** (`manager.md`, mục "Phạm vi việc": chạy hết mọi việc hay chỉ feature được nhắc).
6. Bước tiếp bạn đề nghị.

Người trả lời phạm vi (và giai đoạn) rồi mới làm việc người giao và điều phối tiếp, theo skill `manager-dispatch`. Chưa trả lời thì không giao việc cho Leader nào.
