---
name: codelead-verify-batch
description: Chỉ phiên FEATURE=@CodeLead. Kiểm một lô yêu cầu feature đã ghi done bằng subagent - cl-unity-runner biên dịch và chạy test cả lô, cl-reviewer (mỗi feature một con, song song) đọc test và code đối chiếu yêu cầu, rồi CodeLead chạy gd_req.py verify hoặc reject theo bảng phán quyết. Dùng khi có yêu cầu chờ kiểm (gd_req.py status, tin hay báo cáo của feature).
---
# @CodeLead: kiểm một lô yêu cầu bằng subagent
Việc kiểm là phần tốn nhất của bạn: khoảng một nửa chi phí, gồm đọc code và test, biên dịch, chạy test. Giao cho subagent để ngữ cảnh của bạn chỉ nhận bảng phán quyết.
- Subagent chạy trong chính phiên này: cùng `FEATURE=@CodeLead`, guard ép như với bạn, chung khoá Editor.
- Bạn vẫn là người quyết `verify`/`reject`. Tiêu chí kiểm vẫn ở `.claude/rules/codelead.md`, mục Tiến độ.

## Các bước
1. **Lập lô**: `python Management/tools/gd_req.py status`, rồi `gd_req.py <Tên>` cho từng feature có yêu cầu chờ kiểm. Ghi mỗi feature: danh sách `<node>#<id>` chờ kiểm, tên test đã trích (hoặc `play`), assembly test (`Game.<Tên>.Tests`; test nối dây ở `_Composition/Tests`).
2. **Biên dịch và chạy test**: gọi đúng một subagent `cl-unity-runner`, giao:
   - danh sách assembly hoặc lớp test cần chạy cho cả lô;
   - yêu cầu "biên dịch, đọc console, chạy test, trả về test hỏng và lỗi biên dịch".

   Khoá Editor chỉ một phiên giữ, nên không chạy hai runner cùng lúc. Editor đang bị phiên khác giữ (`~/.coresimple-guard/editor-lock.json`) thì chờ, hoặc kiểm phần đọc code trước.
3. **Đọc test và code**: gọi **song song** các subagent `cl-reviewer`, mỗi feature một con (tối đa 5 con một lượt). Mỗi tin gồm:
   - tên feature;
   - danh sách `<node>#<id>` cùng tên test;
   - test nào runner báo hỏng.

   Reviewer đọc gói thiết kế (`gd_pack.py`), sổ, test và code, rồi trả về bảng theo khuôn ở mô tả của nó.
4. **Quyết**: theo bảng của reviewer và kết quả của runner.
   - `python Management/tools/gd_req.py <Tên> verify <ref>[,<ref>...]` cho các dòng `verify` có test đã qua.
   - `python Management/tools/gd_req.py <Tên> reject <ref> "<lỗi>"` cho dòng `reject` hoặc test hỏng. Lệnh tự gửi tin cho feature.
   - Dòng `hỏi`, hoặc phán quyết bạn chưa tin: tự đọc đúng đoạn reviewer chỉ ra (file, dòng) rồi mới quyết. Không verify mù.
   - Yêu cầu `play`: kiểm bằng Play (bạn, hoặc gom vào một đợt chơi thử theo skill `codelead-dispatch`).
5. **Sau lô**:
   - nối dây cho feature vừa qua (`_Composition`, `BootTests`), theo `codelead.md`, mục Nối dây;
   - `gd_progress.py` khi một node đủ yêu cầu đã kiểm;
   - giao đợt việc tiếp cho các feature vừa xong (skill `codelead-dispatch`).

## Giữ ngữ cảnh nhỏ
- Không dán lại toàn bộ output của subagent vào câu trả lời; chỉ giữ bảng phán quyết và các lỗi đã reject.
- Không tự Read cả file code hay test của feature khi reviewer đã chỉ ra dòng cần xem; Read đúng đoạn đó (offset, limit).
- Xét ai dùng một contract hay asset id: dùng subagent `Explore`, không grep tay khắp `Assets/Features`.
