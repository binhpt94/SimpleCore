---
name: advisor-ask-gpt
description: Chỉ phiên FEATURE=@DesignAdvisor. Một lần hỏi GPT về một node thiết kế, năm bước - đọc node và advice cũ, viết <id>.q.md (dòng also, không tự thêm rào, không chép thiết kế, vòng 2 hỏi _general trước), chạy gd_advise.py, viết note.md, nhắn lại bên hỏi. Dùng mỗi khi người hoặc @GameDesigner nhờ hỏi.
---
# @DesignAdvisor: một lần hỏi GPT
Quyền, ai được nhờ và cách viết vẫn ở `.claude/rules/designadvisor.md`.

## Một lần hỏi
1. Đọc node được hỏi ở `ai/idea` (chưa có thì `live`) và các node nó `based-on`, `refs`. Xem `advice/<node>/` đã hỏi gì, để không hỏi lại.
2. Viết `advice/<node>/<id>.q.md`: câu hỏi cụ thể, một chủ đề; yêu cầu riêng của người hoặc của bên hỏi; bên hỏi muốn gì. Hỏi rộng thì ra góp ý chung chung: thu hẹp trước, hoặc hỏi lại bên nhờ.
   - **Không tự thêm rào cho GPT.** Ràng buộc trong câu hỏi chỉ gồm yêu cầu của **người**, và ghi rõ là người yêu cầu. @GameDesigner và bạn không tự thêm rào thu hẹp góp ý (cấm thêm feature, cấm thêm node, cấm thay ý, "giữ nguyên danh sách hiện có"...). Tin nhờ hỏi có rào như vậy mà không ghi là của người thì bỏ rào đó khi viết câu hỏi và báo lại bên nhờ. Ở vòng 1, 2, 3 (hỏi về node gốc, `_general`, feature, node vòng 3) GPT **được** đề xuất feature mới, node con mới, gộp hoặc tách; lệnh tự dặn GPT điều này. Vòng 4 thì không: GPT chỉ thêm, làm rõ, thay ý trong chính node đó.
   - **Không chép thiết kế vào câu hỏi.** Lệnh tự gửi kèm phần ngữ cảnh (xem bước 3): chép tay thì mỗi lần một kiểu, dễ sót và lệch.
   - **Gom node liên quan:** dòng đầu của file viết `also: feature-upgrade, feature-hud-upgrade` (tối đa 4 node, khác node đang hỏi và node gốc) thì các node đó được gửi đầy đủ như node được hỏi. Dùng khi câu hỏi nằm ở chỗ giao nhau (thành, nâng cấp và thanh nâng cấp; dịch vụ popup và từng popup). Câu trả lời vẫn thuộc về node đang hỏi.
   - **Vòng 2 hỏi chung trước:** lần hỏi đầu của một vòng 2 đặt ở `_general`, về cả bộ feature: có trùng việc, **còn thiếu feature nào**, ranh giới mơ hồ không. Sau đó mới hỏi từng feature, và chỉ hỏi phần sâu của feature đó.
3. Chạy `python Management/tools/gd_advise.py <node> <id>`.
   - **GPT nhận:** câu hỏi; node được hỏi, các node phía trên và các node ở dòng `also:`, viết lại thành tiêu đề, tóm tắt và từng ý kèm **điểm người đã chấm, nghĩa của điểm đó và phản hồi của người** (ý chưa chấm ghi "người chưa chấm"); `CLAUDE.md` của node.
   - **Lệnh tự kèm ngữ cảnh rộng hơn** (không có điểm): bản đồ mọi feature (tên, tóm tắt, tiêu đề các ý); các node cùng cha; mọi node bên dưới node được hỏi, ghi rõ đã chốt ở `live` hay chưa; các node mà `refs` trỏ tới; ý người đã loại ở node này; các góp ý GPT đã cho trước đó về node này và các node phía trên. GPT được dặn không đề xuất ngược thứ đã quyết, không đề xuất lại ý đã loại, không lặp góp ý cũ. Câu hỏi ở `_general` nhận node gốc và bản đồ feature. **Không có điểm AI**: đó là điểm Claude tự chấm cho ý của mình, không đáng tin và sẽ dắt GPT. Đừng chép điểm AI vào câu hỏi.
   - **GPT được dặn:** trước hết **bổ sung** ý node còn thiếu, rồi **làm rõ** ý còn mơ hồ, rồi mới đề xuất **thay** ý sai hoặc yếu; ý đã ổn thì không viết gì.
   - **GPT trả lời:** các góp ý rời, mỗi góp ý **một quyết định**: `### <id góp ý> | <thêm | làm rõ <id ý> | thay <id ý>> | <ý đó trong một câu, tối đa 120 kí tự>`, dưới đó là lí do (tối đa 500 kí tự).
   - Câu trả lời sai dạng, có góp ý quá dài, hoặc trỏ tới id ý không có thì lệnh giữ ở `<id>.rejected.md` và báo lỗi: chạy lại. Hỏi lại cùng id thì bản trước thành `<id>.v<n>.md`.
4. Viết `<id>.note.md` khi có điều bên hỏi cần biết: góp ý trái với thứ đã chốt, dựa trên hiểu sai thiết kế, hoặc trùng ý đã bị người loại (mục "Đã bị người loại" của `gd_status.py`). Đây là nhận xét cho người chấm, **không phải lí do để bỏ góp ý**. Không sửa lời GPT, không thêm ý của riêng bạn như thể là của GPT.
5. Nhắn bên hỏi bằng `SendMessage` (`to: "GameDesigner"`): đường dẫn `advice/<node>/<id>.md`, số góp ý theo từng loại, và có `note` hay không.
