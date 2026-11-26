---
name: gd-art-concept
description: Chỉ phiên FEATURE=@GameDesigner. Ảnh art-concept của node <feature>/cheat - cổng đặt order (node cheat đã ở live, có frame, style vòng 1), khuôn order.md trong codex/orders/_design, studio.py check, người chấm từng ảnh, giao làm lại khi người bảo, ảnh mốc style; claude-concept là của @UIDesigner. Dùng khi đặt hoặc giao làm lại một order art-concept.
---
# @GameDesigner: ảnh art-concept
Luật chung: `.claude/rules/gamedesigner.md`.

## Ảnh concept và node cheat (claude-concept: @UIDesigner đặt; art-concept: bạn đặt)
Frame (của @UIDesigner) rẻ, sửa thoải mái. Ảnh concept có hai loại, cùng cổng, độc lập nhau (art-concept do người chấm, claude-concept không ai chấm): **claude-concept** (@ClaudeImage tự vẽ SVG, `studio.py` đổi ra PNG; không tốn lượt Codex; **giai đoạn IDEA chỉ cần loại này**) và **art-concept** (@Codex gen, tốn lượt, dành cho PROTOTYPE/FULL). Art-concept chỉ đặt ở node `<feature>/cheat` (tối đa 3 state dựng nhanh ở bản dev, bắt buộc khi feature có frame giao diện; luật ở mục "Node cheat" của skill `gd-round-4`). **Claude-concept do @UIDesigner đặt và theo dõi** (mỗi frame một ảnh; luật ở `uidesigner.md`); phần dưới đây là art-concept của bạn. Cổng:
- **Chỉ đặt cho node `cheat` đã ở `live`, có frame**, khi vòng 1 trong `live` đã có style; `studio.py check` từ chối order khác. Mỗi dòng Danh sách là tên một frame của node (ở node cheat: id một state), mỗi frame đúng một ảnh.
- Order nằm ở `Management/studio/codex/orders/_design/<đường dẫn node, '/' thay bằng '.'>/<id>/order.md` (id không trùng với order claude-concept của @UIDesigner), theo khung của `.claude/rules/studio.md` với phần đầu:
  ```
  tool: codex
  kind: art
  count: 1
  deliver: concept
  format: png 1920x1080
  style: ui-flat            # một hay nhiều id style của vòng 1, cách nhau dấu phẩy
  ```
- Viết xong chạy `python Management/tools/studio.py check`. Qua thì order tự vào hộp thư của @Codex (không nhắn bằng SendMessage); không có bước duyệt gen, không chờ người.
- Không xin ảnh mới cho frame còn ảnh dùng được của cùng loại: `check` từ chối. Ảnh mang nhãn "CŨ" (node hay style đã đổi) hoặc bị chấm "Làm lại ngay" thì mới đặt order mới, và chỉ khi thật cần; frame đổi nhỏ không đáng một lần gen.
- **Người chấm từng ảnh art-concept trên trang**: Chưa duyệt (mặc định), Làm lại ngay, Lỗi nhưng chưa cần làm ngay, Đúng phần lớn chi tiết, Đạt; kèm một ô phản hồi. Trang chỉ lưu, không nhắn ai. Bạn không chấm thay người và không ghi được `review.json`.
- **Khi người bảo cho làm lại** (không tự làm khi chưa được bảo): chạy `python Management/tools/studio.py status codex` (dòng `Order art:` ở đầu mỗi tin tóm tắt sẵn). Item ghi `LÀM LẠI ĐƯỢC` ("Làm lại ngay" có phản hồi): giao phiên tool của order đó bằng hộp thư, `python Management/tools/dispatch.py send @Codex --from GameDesigner "Làm lại <order> · <item>: <phản hồi>, …"`; mỗi item làm lại 1 lượt. Item ghi `CHƯA LÀM LẠI ĐƯỢC` (thiếu phản hồi, hoặc đã làm lại đủ 2 lần): không giao phiên tool, báo người item nào và vì sao.
- Ảnh "Đạt" người đặt được làm mốc style trên trang; ảnh mốc được đưa cho Codex ở các lần gen sau cùng style (chỉ ảnh art-concept làm mốc được).
