---
name: gd-user-control
description: Chỉ phiên FEATURE=@GameDesigner. Mode User control - mọi điểm cần quyết thành câu hỏi trong <node>/.ai-need-user-choose/questions.json (khuôn JSON, id, options, multi), dừng chờ, cập nhật node theo answers.json. Dùng khi dòng guard báo mode User control và node có điểm cần người quyết.
---
# @GameDesigner: mode User control
Luật chung: `.claude/rules/gamedesigner.md`.

**User control**: không tự quyết thay người. Mọi điểm cần quyết của node thành câu hỏi trong `<node>/.ai-need-user-choose/questions.json`; rồi dừng chờ. Có câu trả lời (`answers.json`) thì cập nhật `data.md` và `CLAUDE.md` theo đúng lựa chọn của người, kể cả "ý kiến khác". Giữ câu hỏi đã trả lời làm hồ sơ.

```json
{"questions": [{"id": "spawn-side", "text": "Quái vào từ đâu?", "why": "Quyết định bố cục màn chơi.", "multi": false,
  "options": [{"id": "two-sides", "label": "Trái và phải", "detail": "Hợp màn hình ngang, dễ đọc."},
              {"id": "four-sides", "label": "Cả bốn phía", "detail": "Áp lực hơn, khó nhìn trên điện thoại."}]}]}
```
- `id` tiếng Anh, không trùng trong file. `text` một câu hỏi, một ý. `why`: vì sao phải quyết bây giờ.
- `options`: ít nhất 1, nên 2 đến 4; đề xuất bạn thấy hợp nhất đặt đầu, lí do ghi trong `detail`. `multi: true` khi người được chọn nhiều.
- Trang luôn có ô "Ý kiến khác", không cần thêm đề xuất "khác". Đổi nghĩa một câu đã có câu trả lời thì đổi luôn `id`.

## Báo Manager (người quyết 2026-11-27)
Người chỉ chat với Manager, nên mỗi lần ghi câu hỏi mới vào `questions.json` thì báo Manager:
- `python Management/tools/ask.py add --from GameDesigner "<node>: <số> câu ở questions.json" --blocker "User control: node chờ người quyết" --options "<mỗi câu: id, các lựa chọn ngắn>" --suggest "<lựa chọn bạn đề nghị cho từng câu>" --waiting "<node con, frame, feature bị khoá>"`, rồi SendMessage Manager `HỎI NGƯỜI · <id> · <node>`.
- Người trả lời trên trang (`answers.json`) hay qua Manager (`TRẢ LỜI · <id> · …`) đều được. Trả lời qua Manager thì cập nhật node đúng như khi đọc `answers.json`, và ghi câu trả lời vào cột "Đã quyết" trong `CLAUDE.md` của node, kèm "qua Manager, ngày".
- Người đã trả lời trên trang trước: `python Management/tools/ask.py cancel <id> "người đã trả lời trên trang"`.
