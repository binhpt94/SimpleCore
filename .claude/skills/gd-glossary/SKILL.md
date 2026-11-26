---
name: gd-glossary
description: Chỉ phiên FEATURE=@GameDesigner. Bảng thuật ngữ chung ai/idea/glossary - khi nào đưa một từ vào, nghĩa chỉ nói từ là gì, khai glossary#<id>@<hash> trong refs, bỏ một từ, gom định nghĩa trùng. Dùng khi thêm, sửa, bỏ một từ, hoặc gd_status.py (cả cây) nhắc mục Thuật ngữ.
---
# @GameDesigner: bảng thuật ngữ chung
Luật chung: `.claude/rules/gamedesigner.md`.

## Bảng thuật ngữ chung (`glossary`)
- Node `ai/idea/glossary/` nằm ngang hàng các feature (vòng 2) và là nơi duy nhất định nghĩa thuật ngữ. Nó không phải feature: không `code.json`, không node con, không `priority.json`, không tiến độ.
- Mỗi từ một ý: `### wave | AI 8 | Đợt: một nhóm quái vào cùng lúc`. Tiêu đề là `<từ>: <nghĩa>`; chi tiết là ví dụ, phân biệt với từ gần nghĩa. Không có hai ý cùng một từ.
- **Nghĩa chỉ nói từ đó là gì, không nói nó vận hành ra sao.** Luật và con số (ai làm gì, khi nào, bao nhiêu) nằm ở node vòng 3-4 sở hữu nó; chép vào glossary thì một luật nằm hai nơi và sẽ lệch nhau.
- Chỉ đưa một từ vào glossary khi có ít nhất một lí do:
  - từ hiểu được nhiều cách, cần chốt một nghĩa (ví dụ đợt đang đánh có tính lúc thở không; gục khác bị hạ);
  - nhiều node dùng và đã từng định nghĩa khác nhau (cách đánh, mục tiêu);
  - khái niệm riêng của game, người lạ không đoán được (vùng quanh thành, đợt gãy, đội được nhớ).
- Không đưa vào: từ mà tên đã nói đủ nghĩa (tiền hạ quái, nút tạm dừng, tên game); từ mà nghĩa thực chất là luật hay con số; chi tiết giao diện chỉ một node dùng.
- Bỏ một từ: xoá ý trong glossary, gỡ `glossary#<id>@...` khỏi `refs` và dòng `` `glossary#<id>` `` khỏi mục "Định nghĩa" của mọi node dùng nó, rồi chạy `gd_status.py` cho cả cây để chắc không còn chỗ nhắc từ không có.
- Node dùng một từ thì nhắc `` `glossary#wave` `` và khai `glossary#wave@<hash>` trong refs. Đổi nghĩa một từ là đổi quyết định: mọi node khai từ đó thành "cần làm lại"; sửa từ khác thì không ảnh hưởng.
- Chỉ thêm từ khi đã có node dùng. Thấy từ định nghĩa ở hai nơi, hoặc một node định nghĩa lại từ đã có, thì `gd_status.py` (cả cây) nhắc ở mục "Thuật ngữ": gom về glossary, nghĩa khác nhau thì quyết một (User control: hỏi người).
