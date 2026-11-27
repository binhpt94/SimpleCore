---
name: codelead-resolve-issue
description: Chỉ phiên FEATURE=@CodeLead. Xử lý một vấn đề hay báo cáo của worker - việc dừng ở checks-failed, needs-codelead, drift; xét phạm vi ảnh hưởng, tự sửa hay giao lại worker kèm đúng lỗi, biên dịch, test, Play, lỗi kéo dài, xoá thư mục khung. Dùng khi có báo cáo mới của worker hay một vấn đề code.
---
# @CodeLead: xử lý một vấn đề
Tiêu chí mặc định (từ chối khi không thoả) và khuôn BÁO CHẶN ở `.claude/rules/codelead.md`; sổ yêu cầu và tiêu chí kiểm ở skill `codelead-verify-batch`.
Worker (người quyết 2026-11-27) tự đổi khung, contract và mọi feature trong việc của nó; nó không nhờ bạn đổi khung. Bạn nhận báo cáo của nó (server ghi) khi việc xong, dở, hay dừng.

## Quy trình
1. Nhận báo cáo (thấy ở đầu mỗi tin; `FEATURE=@Worker (<F>, việc <id>)`). Xem việc: `python Management/tools/worker.py status <id>` (cổng kiểm, sự kiện, chi phí); đủ hơn ở `Management/workers/<id>/` (`checks.txt`, `result.json`, `events.log`). Báo xong một lô yêu cầu: kiểm theo skill `codelead-verify-batch`.
2. Việc dừng:
   - `checks-failed`: cổng kiểm vẫn hỏng sau một lần tự sửa. Đọc `checks.txt`. Lỗi nhỏ thì tự sửa trong `Assets` sau khi việc đã nạp, hoặc giao lại bằng một tin mới cho feature, nêu đúng lỗi và cách sửa;
   - `needs-codelead`: test hỏng hay Unity báo lỗi biên dịch, mà tự sửa một lần chưa đủ. Lỗi đã nằm trong project chính (code đã nạp): sửa nhanh nếu là lỗi biên dịch (quyền ở `codelead.md`), còn lại giao một tin mới cho worker của feature đó, kèm test hỏng và câu lỗi;
   - `drift`: project chính đổi cùng file từ lúc chụp mà không trộn được. Bấm "Chạy tiếp" ở tab Hộp thư (worker trộn lại), hay `worker.py resume <id>`.
3. Xét phạm vi ảnh hưởng của một contract đổi: ai dùng contract, asset id, asmdef nào. Giao subagent `Explore` tìm khắp `Assets/Features` rồi chỉ đọc đúng đoạn nó chỉ ra.
4. Có lỗi sau khi kiểm hay Play: yêu cầu trong sổ thì `gd_req.py <F> reject <ref> "<lỗi>"` (lệnh tự gửi tin, thành việc worker); việc khác thì `dispatch.py send <F> --from CodeLead "<lỗi, file, việc cần sửa>"`. `dispatch.py send` báo SERVER TẮT: tin đã lưu, nhắn Manager BÁO CHẶN.
5. Lỗi kéo dài (sửa 2 lần chưa xong): phân tích sâu, giao một việc với yêu cầu chính xác (file, thay đổi, thứ tự), có thể gộp các feature liên quan vào một việc.
Xoá thư mục: @CodeLead tự xoá thư mục khung (`Scripts/Contracts`, `Scripts/Internal/Base`, `Scripts/View/Base` và bên dưới) thư mục bên trong `Assets/_Composition`, và mọi file, thư mục của mọi feature, service, kể cả cả thư mục feature (vd feature đã bỏ khỏi thiết kế; không xét cổng code; người quyết 2026-11-27), qua `manage_asset delete`, không cần hỏi. Xoá feature thì gỡ luôn installer, tham chiếu của nó trong `_Composition`, rồi biên dịch, đọc console. Không thuộc @CodeLead: `rules.json`, xoá cả `Assets/_Composition` (hỏi người); đổi luật hay ranh giới (đề xuất cho người).

## Xem việc của worker
- Mọi thứ của một việc nằm ở `Management/workers/<id>/` (`job.json`, `result.json`, `checks.txt`, `applied.json`, `events.log`, `cost.json`). Worktree ở `~/.coresimple-work/<id>` (gỡ bằng nút "Gỡ" hay `worker.py drop <id>`).
