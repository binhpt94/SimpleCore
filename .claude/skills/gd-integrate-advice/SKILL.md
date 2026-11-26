---
name: gd-integrate-advice
description: Chỉ phiên FEATURE=@GameDesigner (@DesignAdvisor đọc để hiểu). Nhờ @DesignAdvisor hỏi GPT (bắt buộc ở vòng 2, vòng 3 khi cần, vòng 1 và 4 không hỏi) và đưa ý GPT vào node - lần hỏi đã lọc (pick.md: ý vào node, node phụ của người, node mới) và lần hỏi cũ (mỗi góp ý một ý), thêm, thay, làm rõ, khai by, gpt-dropped, node-by, không tự thêm rào. Dùng trước khi viết hay sửa một node đã có góp ý, hoặc khi nhờ hỏi GPT.
---
# @GameDesigner: hỏi GPT và đưa ý của GPT vào node
Một lần hỏi bên @DesignAdvisor: skill `advisor-ask-gpt`.

## Nhờ hỏi
Cần GPT cùng nghĩ về một node thì nhờ @DesignAdvisor qua hộp thư (nó chạy theo việc, SendMessage tới nó bị chặn): `python Management/tools/dispatch.py send @DesignAdvisor --from GameDesigner "<node> · <hỏi gì> · <ràng buộc của người nếu có>"`. Nó nhắn lại bạn (SendMessage) đường dẫn file lọc `Management/game-design/advice/<node, '/' thay bằng '.'>/<id>.pick.md`. Bạn chỉ đọc `advice/`.
- **Vòng được hỏi** (người quyết 2026-11-27):
  - **Vòng 2 bắt buộc**: mọi node feature (trừ `glossary`, feature hạ tầng và feature test) phải có ít nhất một lần hỏi GPT trước khi bạn báo node đó xong. Không tự quyết "lần này không hỏi". Node viết từ trước mà chưa hỏi, kể cả đã chốt, thì hỏi bù. `gd_status.py` ghi "CHƯA HỎI GPT"; trang hiện chip đỏ. Viết lại node theo hướng khác hẳn thì hỏi lại.
  - **Vòng 3**: hỏi khi bạn thấy cần.
  - **Vòng 1 (node gốc) và vòng 4: không hỏi.** `gd_advise.py` từ chối.
- **Hai nhóm feature không hỏi GPT** (người quyết 2026-11-26), khai bằng dòng `group:` ở phần đầu `data.md` của node vòng 2 (skill `gd-round-2`):
  - `group: infra`, hạ tầng: không dính gameplay, không phải thứ người chơi nghe, nhìn, cảm nhận (lưu dữ liệu, mã hoá, chuyển scene, nhịp trận). Không cần hỏi; vẫn nhờ hỏi được khi bạn thấy cần.
  - `group: test`, feature dùng để thử (key đuôi `-test` tự là test): không hỏi GPT, kể cả node con của nó.
  - Node cheat (`<feature>/cheat`) của mọi feature: không hỏi GPT.
- **Không tự thêm rào khi nhờ hỏi**: tin nhắn cho `DesignAdvisor` không dặn GPT "đừng đề xuất feature mới", "đừng thêm node" hay rào tương tự, trừ khi người yêu cầu (ghi rõ là người yêu cầu). GPT được đề xuất feature mới, node con mới, gộp, tách.

## Lần hỏi đã lọc (câu trả lời có `filter: pick`; người quyết 2026-11-27)
@DesignAdvisor đã giữ tối đa 5 ý, viết gọn, mỗi ý một node đích, và `gd_pick.py` đã chốt đường đi của từng ý (`<id>.pick.json`). Góp ý GPT bị bỏ không phải việc của bạn.
- **Vào node** (node đích chưa ở `live`): viết ý vào `## Ý cốt lõi` của **node đích** (có thể khác node được hỏi), nhãn ở phần đầu `data.md`: `by: <id ý>=gpt@<thư mục advice>/<lần hỏi>#<id ý giữ>`, ví dụ `enemy-blocker=gpt@feature-hero/r2-3#quai-uu-tien-chan`. `gd_status.py` ghi "Ý GIỮ CHƯA VÀO NODE" ở node đích còn nợ.
- **Node phụ** (node đích đã ở `live`, kể cả đang sửa ở `ai/`): ý chờ người trên trang, chuyển lên hoặc xoá. **Bạn không viết nó vào node**; tự gắn nhãn của nó là lỗi cấu trúc ("AI TỰ CHUYỂN Ý TỪ NODE PHỤ"). Người chuyển lên thì ý vào node với điểm người 8, nhãn do trang ghi; từ đó nó là một ý GPT như mọi ý. Người xoá thì nó là ý bị loại: không đề xuất lại.
- **Node mới** (`Node mới: <cha>/<tên>`): tạo node đó theo luật vòng của nó, khai `node-by: gpt@<thư mục advice>/<lần hỏi>#<id ý giữ>`. `gd_status.py` ghi "NODE MỚI CHƯA TẠO" ở node cha còn nợ.
- Loại `thêm`, `làm rõ <id ý>`, `thay <id ý>` trỏ tới ý của node đích; cách viết như mục dưới.

## Lần hỏi cũ (câu trả lời không có `filter: pick`)
Giữ luật cũ: **mỗi góp ý thành một ý** của chính node được hỏi, không được bỏ góp ý nào (`gd_status.py`: "GÓP Ý CHƯA VÀO NODE"). Nhãn `by: hero-bench=gpt@q1#add-bench` (ý `hero-bench` là góp ý `add-bench` của lần hỏi `q1`). Bạn thấy góp ý sai thì vẫn đưa vào, chấm điểm AI thấp và ghi lí do ở `CLAUDE.md`. Góp ý ở `_general` của lần hỏi cũ: đưa từng góp ý vào feature liên quan (không có nhãn `by:`), hoặc ghi ở `CLAUDE.md` của node gốc vì sao không dùng. Dọn lại các lần hỏi cũ là việc để sau (người quyết 2026-11-27).

## Viết ý của GPT vào node (cả hai loại)
- `thêm`: viết thành ý mới.
- `thay <id ý>`: viết thành ý mới **đặt cạnh** ý bị thay; không xoá ý cũ, không tự chọn một bên. Hai ý hiện chữ cam "trái nhau" cho tới khi điểm loại một bên.
- `làm rõ <id ý>`: viết lại chính ý đó theo lời GPT và gắn nhãn vào nó, hoặc thêm thành ý riêng.
- **Góp ý đụng ý của người** (`user-<n>`, skill `gd-node-format`): không `thay`, không `làm rõ` ý đó; viết thành ý mới đặt cạnh, không đụng ý của người, và hỏi người trong chat nếu hai ý trái nhau.
- **Chỉ được tóm gọn cách trình bày**: tiêu đề và chi tiết được rút gọn, đổi câu cho hợp cách viết của node; nghĩa giữ nguyên. Nguyên văn GPT vẫn nằm trong file advice và trang hiện nó khi mở ý.
- **Chấm điểm như mọi ý.** Full Auto: bạn chấm điểm AI thật cho ý của GPT, không dìm điểm để loại và không nâng điểm ý của mình để thắng. User control: người chấm; bạn không tự quyết bên nào thắng.
- **Loại một ý của GPT** chỉ khi nó bị chấm dưới 5, theo skill `gd-rework-node`: xoá ý và khai `gpt-dropped:` ở phần đầu `data.md` (`<lần hỏi>#<góp ý>=<id ý đã xoá>`; lần hỏi đã lọc: `<thư mục advice>/<lần hỏi>#<id ý giữ>=<id ý đã xoá>`), lí do ghi ở `CLAUDE.md`. User control: máy chỉ nhận khi lịch sử có ý đó bị **người** chấm dưới 5; chưa có thì là lỗi cấu trúc.
- Node tạo ra theo đề xuất của GPT ở lần hỏi cũ: `node-by: gpt@<id lần hỏi>` (lần hỏi nằm ở `advice/` của node cha, của chính node, hoặc ở `_general`). Node đó được chấm như mọi node.
- Ý của chính bạn cũng theo luật ý đơn: một ý một quyết định, hai quyết định thì tách hai ý; `gd_status.py` nhắc "Ý DÀI".
- Thêm ý làm đổi hash data: các node con thành "cần làm lại" như mọi lần node cha đổi.
