---
name: codelead-dispatch
description: Chỉ phiên FEATURE=@CodeLead. Giao việc dev cho tầng dưới theo đợt - đọc gd_req.py status một lần, mỗi feature, service, @Core rảnh lấy một yêu cầu kế tiếp, bỏ việc phải nối tiếp, gửi mọi dispatch.py send trong một lượt; giao đợt chơi thử cho @UnityAutoPlay; xử lý tin BÁO CHẶN của tầng dưới. Dùng khi một feature vừa mở khoá, sau một lô kiểm, khi Manager báo giai đoạn đã xác nhận, hoặc khi tầng dưới báo chặn.
---
# @CodeLead: giao việc cho tầng dưới theo đợt
Từ 2026-11-26 tầng dưới là phần của bạn: feature, service, @Core và tester. @Manager chỉ nắm ba Leader; nó báo bạn giai đoạn đã xác nhận và các quyết định của người về code. Quyền, khung và tiêu chí vẫn ở `.claude/rules/codelead.md`.

## Giao việc theo đợt
0. Chỉ giao việc dev khi giai đoạn đã đặt (guard chặn `dispatch.py send` tới feature, service, @Core khi chưa đặt) và Manager đã báo người xác nhận trong phiên của nó. Chỉ giao việc của node **tới hạn**: yêu cầu ghi "để sau" trong sổ không giao.
1. Đọc **một lần**: `python Management/tools/gd_req.py status` (một dòng mỗi feature) và `python Management/tools/dispatch.py list` (tin chưa giao, feature đang có việc).
   - Không mở sổ chi tiết của từng feature khi chưa cần. Chỉ mở `gd_req.py <Tên>` của feature sắp giao để chọn đúng yêu cầu.
2. Mỗi feature rảnh lấy đúng một việc kế tiếp, theo thứ tự của skill `feature-implement-req`:
   1. yêu cầu bị trả;
   2. đổi thiết kế hoặc mất test;
   3. yêu cầu chưa làm.

   Nêu id yêu cầu `<node>#<id>`, không giao "làm tiếp feature". Có thể giao vài id cùng node trong một tin.
   - Feature ĐƯỢC CODE mà chưa có khung: dựng khung trước (skill `codelead-scaffold-feature`).
   - Ngoại lệ: việc kỹ thuật do khung đòi mà người đã quyết thì giao không cần id; tin nêu "người quyết ngày <ngày>: <câu quyết định>".
3. Bỏ các việc phải nối tiếp:
   - hai feature cùng cần đổi một khung;
   - feature B dùng contract của A mà khung của A chưa có;
   - feature đang chờ bạn sửa khung.
4. **Gửi cùng lúc** mọi việc còn lại trong một lượt, mỗi phiên một lệnh `python Management/tools/dispatch.py send <tên> --from CodeLead "<việc>"`, nối tối đa 6 lệnh bằng `;` trong một lần gọi. Hộp thư có trần số thread bật cùng lúc; tin vượt trần nằm chờ, server tự giao sau.
   - `dispatch.py send` báo **SERVER TẮT** (lỗi 3): tin đã lưu, chưa ai giao. Ngừng giao thêm và nhắn `Manager` BÁO CHẶN (chờ người bật server).
5. Feature báo xong (`done` trong sổ, tin của nó, báo cáo ở đầu tin): kiểm theo skill `codelead-verify-batch`, rồi giao đợt tiếp cho feature đó.

## Chơi thử (nhóm Testers)
- **Bạn (cùng người) là bên duy nhất giao đợt chơi thử.** Tester giữ khoá Unity Editor suốt lúc chơi, cùng khoá với Play và test của bạn, nên bạn tự xếp lịch: không giao khi chính mình đang Play hay chạy test. @GameDesigner, @UIDesigner chỉ gọi nó để giả lập một thao tác (`XÁC NHẬN · …`); việc rộng hơn họ nhờ bạn.
- Giao khi một feature ĐƯỢC CODE đã được kiểm và nối dây ở mức giai đoạn đang làm: feature nào, mức nào, muốn biết điều gì. Gom các thứ cần thử thành một đợt.
- `UnityAutoPlay` chưa chạy (không có trong `ListAgents`): mở bằng `python Management/tools/dispatch.py send @UnityAutoPlay --from CodeLead "<việc>"`. Đang chạy thì `SendMessage`.
- Trước khi giao: Stop Play và lưu scene của mình để nó lấy được khoá.
- Kết quả ở `Tester/UnityAutoPlay/<đợt>/README.md` và báo cáo cuối lượt của nó.
  - Đợt `không đạt`: giao lỗi cho đúng phiên sở hữu qua hộp thư (yêu cầu trong sổ thì `gd_req.py <F> reject`).
  - Sửa xong thì giao chơi lại đúng phần đó.
- Lưu trữ: đợt đã có đợt mới thay thế, hoặc lỗi đã sửa và chơi lại đạt, thì dời bằng `python Management/tools/tester.py archive UnityAutoPlay <đợt>` (hoặc `--keep <N>`).

- **Yêu cầu hẹp** khi test tự động và việc bạn tự Play không phủ được một thao tác (bấm, kéo, một chuỗi thao tác lúc Play): tin theo khuôn `XÁC NHẬN · Feature: … · Thao tác: … (từng bước) · Mong đợi: … · Scene hoặc state cheat: …`.
- **Ai khác gọi được:** @GameDesigner và @UIDesigner chỉ gọi nó cho yêu cầu `XÁC NHẬN` hẹp; việc rộng hơn họ nhờ bạn. @Manager không gọi tester.

## Khi tầng dưới báo chặn
Feature, service, @Core, tester, và tool làm order của feature nhắn bạn `BÁO CHẶN · …` (không nhắn Manager nữa).
1. Lỗi trong vùng của bạn (khung, `_Composition`, khoá Editor bạn giữ): gỡ ngay, rồi báo phiên đó qua hộp thư.
2. Lỗi của phiên khác: giao đúng phiên đó qua hộp thư, kèm file và câu lỗi.
3. Câu hỏi thiết kế: gom các câu của nhiều feature, hỏi `GameDesigner` một tin. Có câu trả lời thì chuyển xuống từng feature.
4. Cần sửa `UnityProject/Packages`, `UnityProject/ProjectSettings`, `Management/tools/rules.json`: hỏi người trong chat, từng lần (vấn đề, đề xuất sửa, ai cần); người trả lời thì gửi lại phiên đó (bước 7).
5. Chỉ người gỡ được (guard, quyền, công cụ, server tắt): nhắn `Manager` một tin BÁO CHẶN gom các chỗ đó; gỡ xong thì báo lại phiên bị chặn (bước 7). Trong lúc chờ, giao việc không phụ thuộc cho feature khác.
6. `BÁO CHẶN · chờ người` (tầng dưới không được hỏi người: chạy headless, không ai trả lời): tự quyết được theo thiết kế, luật thì trả lời luôn; không thì hỏi người trong chat (vấn đề, các phương án, phương án đề nghị, ai bị chặn). Tool (@Codex, @Blender, @ClaudeImage) bị chặn ở order của feature cũng báo bạn.
7. Có câu trả lời: `python Management/tools/dispatch.py send <tên> --from CodeLead "TRẢ LỜI · <việc> · <người quyết gì>"`. Tin mở lượt mới của phiên đó.
8. Không trả lời tin "đã nhận", "cảm ơn"; không chuyển lời qua Manager.
