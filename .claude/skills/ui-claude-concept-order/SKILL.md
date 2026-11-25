---
name: ui-claude-concept-order
description: Chỉ phiên FEATURE=@UIDesigner. Đặt và theo dõi order claude-concept cho node vòng 4 đã ở live (mỗi frame một ảnh), xử lý nhãn chờ ghép lại và CẦN LÀM LẠI bằng studio.py redo, và vẽ frame cho node cheat <feature>/cheat. Dùng khi có node vòng 4 có frame mới lên live, khi trang báo nhãn claude-concept, hoặc khi @GameDesigner viết state cheat.
---
# @UIDesigner: claude-concept và node cheat
Quyền ghi `order.md` và lệnh `studio.py check|status|redo` ở `.claude/rules/uidesigner.md`.

## Claude-concept (bạn đặt và theo dõi)
- Đặt cho mọi node vòng 4 đã ở `live` có frame, mỗi frame một ảnh, khi vòng 1 trong `live` đã có style. Node vòng 4 trước, node cheat sau (cheat do tool ghép từ phần của các node kia; thiếu phần thì dòng `Order art:` nêu node cần đặt thêm).
- @ClaudeImage chỉ vẽ hình mà node tự định (dùng `let` của chính frame); hình `use` số node khác giữ hình cơ bản; nền phủ kín màn bị bỏ qua. `gd_frame.py` in hình nào thuộc loại nào: xếp sai thì sửa `let`/`use`.
- Order ở `Management/studio/claudeimage/orders/_design/<node, '/' thay bằng '.'>/<id>/order.md`, phần đầu `tool: claudeimage`, `kind: art`, `count: 1`, `deliver: concept`, `format: png 1920x1080`, `style: <id style vòng 1>`; mỗi dòng Danh sách là tên một frame. Chạy `studio.py check`, qua thì nhắn ngay `ClaudeImage`.
- Claude-concept không ai chấm. Nhãn "chờ ghép lại" (frame đổi mà phần còn vừa): nhắn @ClaudeImage ghép lại. Nhãn "CẦN LÀM LẠI · cần vẽ: …" hoặc muốn sửa hình: `studio.py redo <ref> <frame> "<lí do>"` rồi nhắn @ClaudeImage.

## Node cheat
Frame của node `<feature>/cheat` cũng do bạn vẽ: đúng một frame cho mỗi state, tên `<id của ý>.frame`, `use` mọi số từ node vòng 4 sở hữu bố cục. State (ý) do @GameDesigner viết; node cheat không có `ui.md` và frame của nó không chấm điểm.
