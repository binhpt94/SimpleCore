---
name: codelead-dispatch
description: Chỉ phiên FEATURE=@CodeLead. Giao việc cho worker theo đợt - đọc gd_req.py status và worker.py status một lần, mỗi feature không có việc worker đang chạy lấy một việc (các yêu cầu mở, hay một việc lớn xuyên feature), gửi mọi dispatch.py send trong một lượt; giao đợt chơi thử cho @UnityAutoPlay; đọc báo cáo của worker. Dùng khi một feature vừa mở khoá, sau một lô kiểm, khi Manager báo giai đoạn đã xác nhận, hoặc khi có báo cáo mới của worker.
---
# @CodeLead: giao việc cho worker theo đợt
Từ 2026-11-27 (người quyết) tầng dưới là worker: feature, service, @Core không còn là phiên. Một tin hộp thư gửi một feature là **một việc worker**: một lượt trong git worktree riêng, sửa được mọi feature, contract, Core, `_Composition`. Server chạy tối đa 3 việc cùng lúc (không hai việc cùng feature), kiểm, nạp, biên dịch, chạy test, ghi `done`, rồi ghi báo cáo cho bạn. Quyền, khung và tiêu chí ở `.claude/rules/codelead.md`; luật của worker ở `.claude/rules/worker.md`.

## Giao việc theo đợt
0. Chỉ giao việc dev khi giai đoạn đã đặt (guard chặn `dispatch.py send` tới feature, service, @Core khi chưa đặt) và Manager đã báo người xác nhận. Chỉ giao việc của node **tới hạn**: yêu cầu ghi "để sau" trong sổ không giao.
1. Đọc **một lần**: `python Management/tools/gd_req.py status` (một dòng mỗi feature), `python Management/tools/worker.py status` (việc đang chạy, chờ, dừng) và `python Management/tools/dispatch.py list` (tin chưa giao).
2. Mỗi feature chưa có việc worker đang chạy lấy **một tin**. Worker tự gói mọi yêu cầu mở của feature (bị trả, đổi thiết kế, mất test, chưa làm), nên tin chỉ cần nêu điều sổ không nói:
   - thứ tự hay trọng tâm (ví dụ "làm các yêu cầu của run-score trước");
   - việc xuyên feature: giao **một lần** cho feature chính, nêu đủ các feature bị kéo theo và contract cần đổi. Worker sửa hết trong cùng việc; không chia nhỏ ra nhiều tin;
   - việc người đã quyết mà sổ không có id: "người quyết ngày <ngày>: <câu quyết định>".
   - **Art Simple:** feature đã chạy đủ bằng GreyBox (yêu cầu của giai đoạn đang làm đã kiểm qua) thì giao một việc `Làm art Simple <F> · <các art, cỡ>`; worker tự vẽ (`.claude/rules/worker.md`, mục Art). Việc này kiểm như mọi việc worker (cổng kiểm, test, `codelead-verify-batch`); Detail là của người.
   - Feature ĐƯỢC CODE mà chưa có thư mục hay khung: dựng khung trước (skill `codelead-scaffold-feature`), hoặc giao luôn cho worker dựng (nó sửa được asmdef, Contracts).
3. **Gửi cùng lúc** mọi tin trong một lượt: `python Management/tools/dispatch.py send <tên> --from CodeLead "<việc>"`, nối tối đa 6 lệnh bằng `;` trong một lần gọi. Tin vượt 3 việc cùng lúc nằm chờ; server tự chạy sau. Tin gửi một feature đang có việc chạy thì thành việc kế tiếp của nó.
   - `dispatch.py send` báo **SERVER TẮT** (lỗi 3): tin đã lưu, chưa ai giao. Ngừng giao thêm và nhắn `Manager` BÁO CHẶN (chờ người bật server).
4. Không theo dõi từng việc. Kết quả tới bằng **báo cáo** ở đầu tin của bạn (`FEATURE=@Worker (<F>, việc <id>)`):
   - `xong (chờ lô test của server)`: server sẽ chạy test và ghi `done`; kiểm lô theo skill `codelead-verify-batch` khi sổ có yêu cầu chờ kiểm;
   - `dở`, `bị chặn` kèm câu hỏi: câu đã vào danh sách của Manager; không cần làm gì, có câu trả lời thì server cho việc chạy tiếp;
   - `bị chặn` vì cổng kiểm hay tự sửa một lần chưa đủ (`checks-failed`, `needs-codelead`): skill `codelead-resolve-issue`.
   - "Công khai đổi" khác `không`: soát contract đổi (tên, nghĩa, ai dùng) lúc kiểm.

## Chơi thử
- Đợt chơi thử là một việc: `python Management/tools/dispatch.py send @UnityAutoPlay --from CodeLead "<feature, mức, muốn biết điều gì>"`. Server xếp nó vào hàng phiếu Editor; tới lượt thì một lượt tester chạy trên project chính, giữ Editor suốt lúc chơi. Không SendMessage tester.
- Giao khi một feature ĐƯỢC CODE đã được kiểm và nối dây ở mức giai đoạn đang làm. Gom các thứ cần thử thành một đợt.
- Kết quả ở `Tester/UnityAutoPlay/<đợt>/README.md` và báo cáo của tester ở đầu tin của bạn.
  - Đợt `không đạt`: giao lỗi cho worker của feature đó qua hộp thư (yêu cầu trong sổ thì `gd_req.py <F> reject`).
  - Sửa xong thì giao chơi lại đúng phần đó.
- Lưu trữ: đợt đã có đợt mới thay thế, hoặc lỗi đã sửa và chơi lại đạt, thì dời bằng `python Management/tools/tester.py archive UnityAutoPlay <đợt>` (hoặc `--keep <N>`).
- @GameDesigner, @UIDesigner tự giao đợt xác nhận hẹp bằng hộp thư (`XÁC NHẬN · Feature: … · Thao tác: … · Mong đợi: … · Scene hoặc state cheat: …`). @Manager không giao.

## Khi unity-mcp mất kết nối
Không giao việc mới cho worker hay tester cho tới khi unity-mcp kết nối lại (worker vẫn chạy được, nhưng lượt nạp và lô test của server cần Unity). Chỉ sửa lỗi biên dịch đang chặn: `python Management/tools/compile_check.py --all`, sửa (`codelead.md`, quyền sửa code feature), rồi `refresh_unity`. Việc thiết kế, frame của Leader khác không bị ảnh hưởng.

## Câu hỏi, chỗ chặn
- Worker không nhắn bạn và không BÁO CHẶN: câu hỏi của nó vào thẳng `ask.py` (Manager hỏi người). Bạn thấy nó ở dòng "Cần bên khác" của báo cáo.
- Cần sửa `UnityProject/Packages`, `UnityProject/ProjectSettings`, `Management/tools/rules.json` (worker ghi trong câu hỏi): hỏi người qua Manager, từng lần (`ask.py add … --options "<đúng đường dẫn và thay đổi>"`). Người cho phép thì bạn tự sửa, rồi giao worker chạy tiếp.
- Chỉ người gỡ được (guard, quyền, công cụ, server tắt): nhắn `Manager` một tin BÁO CHẶN gom các chỗ đó. Trong lúc chờ, giao việc không phụ thuộc cho feature khác.
- Không trả lời tin "đã nhận", "cảm ơn".
