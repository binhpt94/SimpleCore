# Đề xuất: thêm lệnh huỷ order vào studio.py

Người đề xuất: @Manager · 2026-11-25 · Chờ người duyệt

## Vấn đề
Người quyết huỷ hai order sprite Battlefield/battlefield-sprites (5 item) và Hud/hud-sprites (7 item). Hai order này trùng các sprite đã chấm Đạt và còn ghi style cũ. Nhưng không bên nào huỷ được:
- `studio.py` không có lệnh huỷ. Các lệnh hiện có: check, status, measure, codex, consult, blender, claude, redo, deliver, review, anchor, pick, debt close.
- Feature chỉ được tạo mới `order.md`, không sửa được `order.md` hay `status.json` đã có.
- Tin báo order đã rời hộp thư, nên `dispatch.py cancel` không dùng được.

Hiện cách tạm là @Manager bảo @ClaudeImage bỏ qua hai order. Nhưng order vẫn ở trạng thái "mới" trên tab Studio, `studio.py check` có thể đưa lại chúng vào hộp thư.

## Đề xuất
- **A:** thêm `studio.py cancel <F>/<id> "<lí do>"`. Feature sở hữu order, @Manager và người được chạy. Order chuyển sang trạng thái "đã huỷ", `check` bỏ qua, tab Studio hiện lí do.
- **B:** người tự huỷ trên tab Studio, bằng một nút có PIN.

## Ảnh hưởng
Không đụng code game. Chỉ đổi `Management/tools/studio.py`, trang Studio và luật trong `.claude/rules/studio.md`.
