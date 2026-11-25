---
name: gd-integrate-advice
description: Chỉ phiên FEATURE=@GameDesigner (@DesignAdvisor đọc để hiểu). Nhờ @DesignAdvisor hỏi GPT và đưa góp ý của GPT vào node - bắt buộc hỏi ở vòng 1, 2, mỗi góp ý thành một ý (thêm, thay, làm rõ), khai by, gpt-dropped, node-by, góp ý ở _general, không tự thêm rào. Dùng trước khi viết hay sửa một node đã có góp ý, hoặc khi nhờ hỏi GPT.
---
# @GameDesigner: hỏi GPT và đưa góp ý vào node
Một lần hỏi bên @DesignAdvisor: skill `advisor-ask-gpt`.

## Bên @GameDesigner: đưa góp ý vào node
Cần GPT cùng nghĩ về một node thì nhắn `DesignAdvisor` (`SendMessage`, `to: "DesignAdvisor"`): node nào, hỏi gì, ràng buộc nào phải giữ. Nó nhắn lại đường dẫn `Management/game-design/advice/<node, '/' thay bằng '.'>/<id>.md` (node gốc: `_root`). Bạn chỉ đọc `advice/`.
- **Hỏi GPT là bắt buộc ở vòng 1 và vòng 2**: node gốc và mọi node feature (trừ `glossary`) phải có ít nhất một lần hỏi GPT trước khi bạn báo node đó xong. Không tự quyết "lần này không hỏi". Node viết từ trước mà chưa hỏi, kể cả đã chốt, thì hỏi bù. `gd_status.py` ghi "CHƯA HỎI GPT" ở node còn nợ và liệt kê ở mục "Chưa hỏi GPT" cuối bản in; trang hiện chip đỏ. Viết lại node theo hướng khác hẳn thì hỏi lại. Vòng 3, 4 không bắt buộc.
- **Đã có góp ý thì phải đưa hết vào.**
- **Mỗi góp ý thành một ý** trong `## Ý cốt lõi` của `data.md`, và khai nguồn ở phần đầu (không tính vào hash):
  `by: hero-bench=gpt@q1#add-bench, hero-four=gpt@q1#four-heroes` (ý `hero-bench` là góp ý `add-bench` của lần hỏi `q1`).
  - `thêm`: viết thành ý mới.
  - `thay <id ý>`: viết thành ý mới **đặt cạnh** ý bị thay; không xoá ý cũ, không tự chọn một bên. Hai ý hiện chữ cam "trái nhau" cho tới khi điểm loại một bên.
  - `làm rõ <id ý>`: viết lại chính ý đó theo lời GPT và gắn nhãn vào nó (`hero-count=gpt@q1#clear-count`), hoặc thêm thành ý riêng.
- **Chỉ được tóm gọn cách trình bày**: tiêu đề và chi tiết của ý được rút gọn, đổi câu cho hợp cách viết của node; nghĩa của góp ý giữ nguyên. Nguyên văn GPT vẫn nằm trong file góp ý và trang hiện nó khi mở ý.
- **Không được bỏ góp ý nào**, ở cả hai mode. Góp ý chưa thành ý là lỗi cấu trúc của node (`gd_status.py`: "GÓP Ý CHƯA VÀO NODE"); bạn thấy góp ý sai thì vẫn đưa vào, chấm điểm AI thấp và ghi lí do ở `CLAUDE.md`.
- **Chấm điểm như mọi ý.** Full Auto: bạn chấm điểm AI thật cho ý của GPT, không dìm điểm để loại và không nâng điểm ý của mình để thắng. User control: người chấm; bạn không tự quyết bên nào thắng.
- **Loại một ý của GPT** chỉ khi nó bị chấm dưới 5, theo skill `gd-rework-node`: xoá ý và khai `gpt-dropped: q1#add-bench=hero-bench` ở phần đầu `data.md` (`<lần hỏi>#<góp ý>=<id ý đã xoá>`), lí do ghi ở `CLAUDE.md`. User control: máy chỉ nhận khi lịch sử có ý đó bị **người** chấm dưới 5; chưa có thì là lỗi cấu trúc. Trang và `gd_status.py` đếm số ý GPT bị loại.
- **Góp ý ở `_general`** (câu hỏi chung của vòng 2) không gắn node nào nên máy không kiểm: bạn đưa từng góp ý vào feature liên quan (viết thành ý của feature đó, không có nhãn `by:`), hoặc ghi ở `CLAUDE.md` của node gốc vì sao không dùng. Không bỏ lặng lẽ.
- **Không tự thêm rào khi nhờ hỏi**: tin nhắn cho `DesignAdvisor` không dặn GPT "đừng đề xuất feature mới", "đừng thêm node" hay rào tương tự, trừ khi người yêu cầu (ghi rõ là người yêu cầu). Ở vòng 1, 2, 3 GPT được đề xuất feature mới, node con mới, gộp, tách; vòng 4 thì không.
- Feature hay node tạo ra theo đề xuất của GPT: tạo node, thêm `node-by: gpt@<id lần hỏi>` (lần hỏi nằm ở `advice/` của node cha, của chính node, hoặc ở `_general`); node đó được chấm như mọi node.
- Ý của chính bạn cũng theo luật ý đơn: một ý một quyết định, hai quyết định thì tách hai ý; `gd_status.py` nhắc "Ý DÀI".
- Thêm ý làm đổi hash data: các node con thành "cần làm lại" như mọi lần node cha đổi.
