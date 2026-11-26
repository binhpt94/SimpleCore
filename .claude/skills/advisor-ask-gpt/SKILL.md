---
name: advisor-ask-gpt
description: Chỉ phiên FEATURE=@DesignAdvisor. Một lần hỏi GPT về một node thiết kế vòng 2 hoặc 3, sáu bước - đọc node và advice cũ, viết <id>.q.md (dòng also, không tự thêm rào, không chép thiết kế, vòng 2 hỏi _general trước), chạy gd_advise.py, lọc góp ý vào <id>.pick.md (tối đa 5 ý, mỗi ý một node đích, lí do bỏ cụ thể) và chạy gd_pick.py, viết note.md, nhắn lại bên hỏi. Dùng mỗi khi người hoặc @GameDesigner nhờ hỏi.
---
# @DesignAdvisor: một lần hỏi GPT
Quyền, ai được nhờ và cách viết vẫn ở `.claude/rules/designadvisor.md`.

## Hỏi ở vòng nào (người quyết 2026-11-27)
- Vòng 2 (node feature) và `_general`: bắt buộc, như trước.
- Vòng 3: khi @GameDesigner thấy cần và nhờ.
- Vòng 1 (node gốc, `_root`) và vòng 4: không hỏi. `gd_advise.py` từ chối. Được nhờ thì nhắn lại một câu theo luật này.

## Một lần hỏi
1. Đọc node được hỏi ở `ai/idea` (chưa có thì `live`) và các node nó `based-on`, `refs`. Xem `advice/<node>/` đã hỏi gì, để không hỏi lại.
2. Viết `advice/<node>/<id>.q.md`: câu hỏi cụ thể, một chủ đề; yêu cầu riêng của người hoặc của bên hỏi; bên hỏi muốn gì. Hỏi rộng thì ra góp ý chung chung: thu hẹp trước, hoặc hỏi lại bên nhờ.
   - **Không tự thêm rào cho GPT.** Ràng buộc trong câu hỏi chỉ gồm yêu cầu của **người**, và ghi rõ là người yêu cầu. @GameDesigner và bạn không tự thêm rào thu hẹp góp ý (cấm thêm feature, cấm thêm node, cấm thay ý, "giữ nguyên danh sách hiện có"...). Tin nhờ hỏi có rào như vậy mà không ghi là của người thì bỏ rào đó khi viết câu hỏi và báo lại bên nhờ. GPT **được** đề xuất feature mới, node con mới, gộp hoặc tách; lệnh tự dặn GPT điều này.
   - **Không chép thiết kế vào câu hỏi.** Lệnh tự gửi kèm phần ngữ cảnh (xem bước 3): chép tay thì mỗi lần một kiểu, dễ sót và lệch.
   - **Gom node liên quan:** dòng đầu của file viết `also: feature-upgrade, feature-hud-upgrade` (tối đa 4 node, khác node đang hỏi và node gốc) thì các node đó được gửi đầy đủ như node được hỏi. Dùng khi câu hỏi nằm ở chỗ giao nhau (thành, nâng cấp và thanh nâng cấp; dịch vụ popup và từng popup).
   - **Vòng 2 hỏi chung trước:** lần hỏi đầu của một vòng 2 đặt ở `_general`, về cả bộ feature: có trùng việc, **còn thiếu feature nào**, ranh giới mơ hồ không. Sau đó mới hỏi từng feature, và chỉ hỏi phần sâu của feature đó.
3. Chạy `python Management/tools/gd_advise.py <node> <id>`.
   - **GPT nhận:** câu hỏi; node được hỏi, các node phía trên và các node ở dòng `also:`, viết lại thành tiêu đề, tóm tắt và từng ý kèm **điểm người đã chấm, nghĩa của điểm đó và phản hồi của người** (ý chưa chấm ghi "người chưa chấm"); `CLAUDE.md` của node.
   - **Lệnh tự kèm ngữ cảnh rộng hơn** (không có điểm): bản đồ mọi feature (tên, tóm tắt, tiêu đề các ý); các node cùng cha; mọi node bên dưới node được hỏi, ghi rõ đã chốt ở `live` hay chưa; các node mà `refs` trỏ tới; ý người đã loại ở node này, kể cả ý GPT người đã xoá ở phần bổ sung; các góp ý GPT đã cho trước đó về node này và các node phía trên. **Không có điểm AI**: đó là điểm Claude tự chấm cho ý của mình, không đáng tin và sẽ dắt GPT. Đừng chép điểm AI vào câu hỏi.
   - **GPT trả lời** tự do trong khuôn: các góp ý rời, mỗi góp ý một quyết định, `### <id góp ý> | <thêm | làm rõ <id ý> | thay <id ý>> | <ý trong một câu>`, dưới đó là lí do.
   - Câu trả lời sai dạng thì lệnh giữ ở `<id>.rejected.md` và báo lỗi: chạy lại. Hỏi lại cùng id thì bản trước thành `<id>.v<n>.md`. Câu trả lời mới mang dòng `filter: pick`: nó phải qua bước 4 mới vào thiết kế.
4. **Lọc** (người quyết 2026-11-27). GPT hay cho nhiều góp ý trùng nhau và đặt việc của feature khác vào node được hỏi. Bạn chọn, viết lại, và đặt đúng chỗ.
   - Đọc mọi góp ý; mục "Đã bị người loại" và "Người đã xoá" của `gd_status.py ai/idea/<node đích>`; các ý đã có của node đích.
   - **Giữ tối đa 5 ý** cho cả lần hỏi. Ít hơn cũng được, kể cả 0; không cố cho đủ. Ưu tiên ý ít trùng nhất với ý đã có, ý người đã loại và các góp ý khác. Góp ý cùng hệ quả thì gộp thành một ý (dòng `nguồn:` liệt kê cả).
   - **Node đích là feature làm việc đó**, không phải node được hỏi. Góp ý viết "feature-enemy làm…" thì đích là `feature-enemy` (hay node vòng 3 của nó). Đích chỉ là node vòng 2, 3 có trong `ai/idea`, hoặc `Node mới: <cha>/<tên>` ở vòng 2, 3. Ý là chi tiết của vòng 4 (con số, trường hợp biên) thì bỏ, lí do ghi "chi tiết vòng 4, để `<node>`".
   - **Viết ngắn, dễ hiểu**, giữ nghĩa của GPT: tiêu đề là một câu khẳng định; chi tiết một hai câu. Không thêm ý riêng của bạn.
   - **Lí do bỏ phải cụ thể, có id**, để người đọc lướt là biết có đáng xem lại: `trùng \`feature-enemy#enemy-path\``, `trùng ý người đã loại \`<node>#<ý>\``, `trùng góp ý \`<id>\``, `chi tiết vòng 4, để \`<node>\``. Không ghi chung chung "yếu", "không cần".
   - Khuôn `advice/<node>/<id>.pick.md`:
     ```
     ## Giữ
     ### <id ý> | <node đích hoặc "Node mới: <cha>/<tên>"> | <thêm | làm rõ <id ý> | thay <id ý>> | <tiêu đề ngắn>
     nguồn: <id góp ý>, <id góp ý>
     <chi tiết ngắn>

     ## Bỏ
     - <id góp ý>: <lí do cụ thể>
     ```
     `làm rõ` và `thay` trỏ tới một ý **của node đích**. "Node mới" chỉ dùng `thêm`. Mỗi góp ý có mặt đúng một lần (ở `nguồn:` hoặc ở `## Bỏ`).
   - Chạy `python Management/tools/gd_pick.py <node> <id>`. Lỗi thì sửa file lọc rồi chạy lại; lệnh không ghi gì khi còn lỗi. Đúng thì lệnh ghi `<id>.pick.json` và in đường đi của từng ý, chốt tại lúc đó:
     - **vào node**: node đích chưa ở `live`. @GameDesigner viết ý vào node đó.
     - **node phụ**: node đích đã ở `live`. Ý chờ người lọc trên trang (chuyển lên hoặc xoá). Không ai khác viết nó vào node.
     - **node mới**: @GameDesigner tạo node đó.
   - Ý người đã chuyển lên hay đã xoá thì giữ nguyên trong file lọc; sửa hay bỏ nó thì lệnh từ chối.
5. Viết `<id>.note.md` khi có điều bên hỏi cần biết: góp ý trái với thứ đã chốt, dựa trên hiểu sai thiết kế. Không sửa lời GPT, không thêm ý của riêng bạn như thể là của GPT.
6. Nhắn bên hỏi bằng `SendMessage` (`to: "GameDesigner"`): đường dẫn `advice/<node>/<id>.pick.md`, số ý giữ theo node đích và đường đi (vào node, node phụ, node mới), số góp ý bỏ, có `note` hay không.
