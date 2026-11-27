---
name: gd-rework-node
description: Chỉ phiên FEATURE=@GameDesigner. Làm lại một node mà gd_status.py ghi cần làm lại - ý bị chấm dưới 5, lệch hash với cha, chưa có ý, ref đổi hoặc thiếu; đọc lịch sử ý đã bị người loại trước khi viết lại; làm từ trên xuống. Dùng khi gd_status.py báo một node cần làm lại.
---
# @GameDesigner: làm lại một node
Lúc node đạt ở `.claude/rules/gamedesigner.md`; khuôn node ở skill `gd-node-format`; chấm điểm ở skill `gd-scoring`.

## Làm lại một node
`gd_status.py` ghi rõ node nào "bạn cần làm lại" và vì sao. Có các lí do sau:
1. **Ý bị chấm dưới 5** (người hoặc AI): đọc phản hồi của người, viết lại đúng ý đó trong `data.md` (giữ `id`), chấm lại điểm AI. Đừng đụng các ý đã đạt.
2. **Lệch hash với cha** (data của cha đã đổi): đọc data mới của cha, rà từng ý của node, sửa ý nào không còn khớp, rồi ghim lại. Ghim bằng `python Management/tools/gd_pin.py ai/idea/<node> [--chain]` in diff (ý thêm, bỏ, viết lại của node cha và node được ref, so với bản đã ghim); rà, sửa ý nào không còn khớp, rồi chạy lệnh `--write <mã>` ở dòng cuối để ghim. Không sửa `based-on` hay `refs` bằng tay. Hash đổi dây chuyền (cha → con → cheat) thì dùng `--chain` cho cả nhánh; node có nhánh khác ref tới thì `<node> --all`; node gốc đổi thì `gd_pin.py --all` (cả cây, một mã). Diff in mỗi thay đổi nguồn một lần; `--hide <id,...>` ẩn phần đã rà. Sau khi người xoá node: `gd_impact.py <node>` liệt kê mọi chỗ còn nhắc tới nó, dọn xong thì `gd_pin.py --all --drop-dead`. Bỏ hay sửa ý theo id: `gd_idea.py rm|replace ai/idea/<node>#<id>`, không sửa tay. Rà xong không phải sửa ý nào thì chỉ ghim; ghim lại không làm node phải chốt lại.
3. **Chưa có ý nào**: viết các ý cốt lõi và chấm điểm.
4. **Ref đã đổi hoặc thiếu ref**: đọc node đích (cả `CLAUDE.md`) hoặc từ trong glossary, rà các ý dùng nó, sửa ý nào không còn khớp (con số, tên, luật), rồi ghi hash mới vào `refs`. Rà xong không phải sửa gì thì chỉ đổi hash.

5. **Node vòng 3 trống sau khi xoá** (người xoá mọi node vòng 4 của nó): viết node vòng 4 mới theo node vòng 3, hoặc hỏi người qua Manager (`ask.py`) có xoá luôn node vòng 3 không. Tới lúc đó feature bị khoá code.

Trước khi viết lại, **đọc lịch sử của node** (mục "Đã bị người loại" trong `gd_status.py`, hoặc `.history/log.jsonl`): không đề xuất lại ý đã bị loại hay một biến thể gần giống, và không đưa lại ý người đã xoá (mục "Người đã xoá": xoá trung tính, chỉ là không cần); nếu thấy vẫn cần ý đó thì hỏi người qua Manager (`ask.py`), nêu rõ nó đã bị loại lúc nào và vì sao lần này khác.
Làm lại từ trên xuống: node cha đạt rồi mới tới node con.
