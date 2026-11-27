---
name: ui-layout-screen
description: Chỉ phiên FEATURE=@UIDesigner. Xếp bố cục một màn hay popup cho bớt máy móc - đọc ui-lessons.md và ảnh tham chiếu, hai phương án (alt), vòng tự xem gd_frame.py --png với bốn câu hỏi, thang khoảng cách và cỡ, thứ bậc nút, dòng why, ghi bài học. Dùng khi xếp mới, xếp lại hoặc làm lại frame bị chấm thấp.
---
# @UIDesigner: xếp một màn
Cú pháp frame: skill `ui-frame-syntax`. Quyền, `ui.md`, luật khớp `ui.md` và luật bố cục vẫn ở `.claude/rules/uidesigner.md`.

## Quy trình một màn (để bố cục không máy móc)
1. **Đầu lượt** đọc `Management/game-design/ai/ui-lessons.md` và chạy `references.py`; Read ảnh `bố cục` cùng tag với màn (HUD trận, popup thưởng...) cùng nhận định của người.
2. **Phương án**: màn mới hoặc đổi nhiều thì làm 2 frame: một bản an toàn theo luật và tham chiếu, một bản táo bạo hơn (nhấn mạnh hơn, bất đối xứng, bố cục khác hẳn). Bản thứ hai ghi `alt: <tên frame thứ nhất>` ở phần đầu, tên file khác (`main-bold.frame`). Màn đổi nhỏ thì một frame. Người chọn một bằng cách chấm từ 5; sau đó `gd_status.py` ghi `CHỜ @UIDesigner: … xoá các phương án còn lại`: xoá bằng `gd_delete.py`, và bỏ dòng `alt:` của bản được chọn nếu nó là bản `alt`.
3. **Vòng tự xem** (bắt buộc, 2–3 vòng trước khi báo xong): `gd_frame.py ai/idea/<node> --png`, Read `<tên>.png` (chỉ chữ người chơi đọc) và `<tên>.measure.png` (có số), trả lời bốn câu rồi sửa:
   - Mắt rơi vào đâu đầu tiên, có đúng phần tử chính không?
   - Nút hành động chính có nổi nhất không (cỡ, vị trí, khoảng trống quanh nó)?
   - Có chỗ nào chật, hay trống vô lý?
   - Nhóm có rõ không: khoảng trong nhóm nhỏ hẳn khoảng giữa các nhóm?
4. Ghi câu trả lời cuối thành các dòng `why:`, có một dòng `why: theo tham chiếu <id>: …` (hoặc `why: không có tham chiếu phù hợp`). Báo cáo ghi số vòng đã xem ở dòng "Kiểm tra".

## Thang và thứ bậc (gamedesign.py kiểm, cảnh báo không chặn: xử lý hoặc giải thích ở `why:`)
- Khoảng cách theo thang **8 / 16 / 24 / 40 / 64 px** (lệch tối đa 2 px). Trong nhóm dùng bậc nhỏ, giữa các nhóm nhảy ít nhất hai bậc.
- Ba cấp nút: chính > phụ > nhỏ, mỗi cấp hơn cấp dưới **ít nhất 1,4 lần diện tích**. Cỡ chữ theo thang 24 / 32 / 40 / 56 / 72 px (chữ chính, giá trị lớn, tiêu đề).
- Mỗi màn **đúng một nút hành động chính**; chừa khoảng thở quanh nó (ít nhất một bậc 40). Bất đối xứng được, khi có lý do ở `why:`.
- Số chung (thang, cỡ nút từng cấp, lề an toàn) khai `let` một lần ở node bố cục chung, node khác `use` và khai refs; không chép số.
- Cảnh báo tự động: hơn một nút chính; nút chính không lớn hơn hẳn các nút khác; từ 3 nút cùng cỡ; khoảng cách lệch thang; mọi khoảng cách bằng nhau (lưới tăm tắp, trông máy móc).

## Bài học (`Management/game-design/ai/ui-lessons.md`)
- Mỗi frame bị người chấm dưới 5, hoặc có phản hồi, thì thêm một dòng: `- <ngày> · <node>/<frame> · người: "<phản hồi>" · bài học: <luật ngắn, làm theo được>`.
- Bài học lặp lại 2–3 lần: nhắn `Manager` đề xuất đưa thành luật trong `uidesigner.md` hoặc skill này (file `.claude` chỉ người sửa); vẫn giữ dòng trong `ui-lessons.md`.
