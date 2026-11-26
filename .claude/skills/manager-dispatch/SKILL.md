---
name: manager-dispatch
description: Chỉ phiên FEATURE=@Manager. Điều phối một đợt việc giữa ba Leader (CodeLead, GameDesigner, UIDesigner) - lập bảng theo Leader, mỗi Leader một việc kế tiếp, nhắn cùng lúc; điều phối lại khi một Leader nhắn BÁO CHẶN hay HẾT CHẶN. Tầng dưới (feature, service, @Core, tool, tester) là của CodeLead, không giao ở đây. Dùng sau kiểm tra đầu phiên, khi có báo cáo mới của Leader, hoặc khi nhận tin BÁO CHẶN.
---
# @Manager: giao việc cho ba Leader theo đợt
Quyền, phạm vi (chỉ ba Leader, người quyết 2026-11-26), chuỗi của một feature và danh sách "phải nối tiếp" vẫn ở `.claude/rules/manager.md`. Kiểm tra đầu phiên: skill `manager-startup-check`.

## Giao việc theo đợt
0. Người đã xác nhận giai đoạn đang làm trong phiên này (skill `manager-startup-check`) và bạn đã báo `CodeLead`. Chỉ nhắc việc của node **tới hạn**; feature để sau còn dừng ở vòng 2, 3 thì không giục @GameDesigner đào sâu.
1. Lập bảng theo Leader:
   - **@GameDesigner:** node đang viết, node chờ người chấm hay chốt, feature ĐANG KHOÁ vì thiếu vòng 4 (`gd_status.py`, mục "Cổng code theo feature").
   - **@UIDesigner:** các dòng `CHỜ @UIDesigner` trong `gd_status.py`.
   - **@CodeLead:** `gd_req.py status` (chờ kiểm, bị trả, đổi thiết kế; feature ĐƯỢC CODE mà chưa có yêu cầu đã làm) và báo cáo mới của nó.
2. Mỗi Leader lấy đúng một việc kế tiếp, ưu tiên việc đang chặn Leader khác. Ví dụ: feature vừa mở khoá thì nhắn `CodeLead` dựng khung và giao việc; `ui.md` mới thì nhắn `UIDesigner`; CodeLead hỏi thiết kế thì chuyển `GameDesigner` nếu CodeLead chưa nhắn thẳng.
   - Không giao việc cho từng feature, không nêu id yêu cầu thay CodeLead: chọn yêu cầu nào, giao feature nào là việc của nó (skill `codelead-dispatch`).
3. Bỏ các việc vướng danh sách "phải nối tiếp". **Nhắn cùng lúc** mọi Leader còn lại trong một lượt bằng `SendMessage`. Không chờ Leader này trả lời rồi mới nhắn Leader kia.
4. Báo người bảng đó và các việc đang chờ người: chấm, chọn xung đột, chốt, và các câu bạn đã hỏi mà người chưa trả lời (mục "Hỏi người" của `manager.md`). Đây thường là thứ đang chặn nhiều việc nhất.
5. Có báo cáo mới của một Leader thì lập lại bảng và nhắn đợt tiếp. Báo cáo của tầng dưới không tới bạn; CodeLead đọc.

## Không nhắn thừa
- Không nhắn để xác nhận đã nhận, cảm ơn, hay hỏi "xong chưa" khi báo cáo và `gd_req.py status` đã cho biết.
- Hai Leader cần nhau (ví dụ CodeLead hỏi thiết kế, GameDesigner đổi `ui.md`) thì họ nhắn thẳng nhau; bạn chỉ chen vào khi họ bế tắc hoặc cần người quyết.
- Người quyết một điều về code: nhắn `CodeLead` một tin; CodeLead tự báo xuống các feature.

## Khi một Leader báo chặn
@CodeLead, @GameDesigner, @UIDesigner nhắn `BÁO CHẶN · <lỗi | chờ người> · …` khi gặp lỗi không tự gỡ được hoặc phải dừng chờ người, và `HẾT CHẶN · <việc>` khi đi tiếp được. Nhận tin thì điều phối lại ngay, không đợi báo cáo cuối lượt:
1. Ghi vào bảng theo Leader: bị chặn ở đâu, chờ ai, kéo theo những gì.
2. **Dừng nhắn các việc phụ thuộc** vào thứ đang bị chặn. Ví dụ: @GameDesigner chờ người chốt node cha thì không giục @UIDesigner xếp frame cho node con.
3. **Nhắn ngay đợt mới cho các Leader không phụ thuộc.** Một Leader đứng không phải là cả project đứng.
4. **Chờ người**: gom mọi việc đang chờ người thành một danh sách ngắn trong câu trả lời: việc gì, làm ở đâu (tab, node), đang chặn những gì; việc chặn nhiều nhất xếp trước. Không để từng Leader hỏi người lẻ tẻ.
5. **Lỗi mà Leader khác gỡ được**: nhắn đúng Leader đó, kèm file và câu lỗi. Lỗi chỉ người gỡ được (guard, quyền, công cụ, hạn mức, server tắt): báo người, nêu rõ lệnh và câu lỗi.
6. Nhận `HẾT CHẶN`: lập lại bảng và nhắn đợt tiếp.

Leader im lặng lâu mà bảng cho thấy nó đang giữ việc: hỏi thẳng nó trước khi coi là bị chặn.
